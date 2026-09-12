using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;
using System.Text.Json;

namespace ResilientCheckout.Notifications
{
    // Unlike ServiceBusOutboxRelay (which does POLLING every N seconds against the
    // database), this worker is PUSH-based: it subscribes to the "notifications"
    // Subscription and Azure Service Bus delivers messages to us via HandleMessageAsync
    // as soon as they arrive. There's no table or ProcessedAt column to query here -- the
    // "state of what's already been processed" is carried by Service Bus itself (the lock + the ack).
    public class Worker : BackgroundService
    {
        private readonly ServiceBusProcessor _processor;
        private readonly NotificationDispatcher _dispatcher;
        private readonly ILogger<Worker> _logger;

        public Worker(
            ServiceBusClient client,
            IConfiguration configuration,
            NotificationDispatcher dispatcher,
            ILogger<Worker> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;

            var topicName = configuration["ServiceBus:PaymentEventsTopic"]
                ?? throw new InvalidOperationException("ServiceBus:PaymentEventsTopic is not configured.");
            var subscriptionName = configuration["ServiceBus:NotificationsSubscription"]
                ?? throw new InvalidOperationException("ServiceBus:NotificationsSubscription is not configured.");

            // AutoCompleteMessages = false: we want to explicitly decide when a
            // message is completed (success) or abandoned (failure -> Service Bus
            // redelivers it). MaxConcurrentCalls = 1 so the demo is deterministic/ordered.
            _processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += HandleErrorAsync;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _processor.StartProcessingAsync(stoppingToken);

            try
            {
                // The processor delivers messages on its own internal threads; this method
                // just needs to stay "alive" while the host isn't asking to stop.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the host cancels stoppingToken while shutting down the worker.
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var payload = args.Message.Body.ToObjectFromJson<PaymentEventPayload>()
                    ?? throw new JsonException($"Message {args.Message.MessageId} does not have a valid payload.");

                var request = new NotificationRequest
                {
                    OrderId = payload.OrderId,
                    TransactionId = payload.TransactionId,
                    Message = $"Your payment for order #{payload.OrderId} was processed successfully (transaction {payload.TransactionId})."
                };

                await _dispatcher.DispatchAsync(request, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message {MessageId}; abandoning for retry.",
                    args.Message.MessageId);
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Error in the ServiceBusProcessor ({ErrorSource}).", args.ErrorSource);
            return Task.CompletedTask;
        }
    }
}
