using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;
using System.Text.Json;

namespace ResilientCheckout.Notifications
{
    // A diferencia de ServiceBusOutboxRelay (que hace POLLING cada N segundos sobre la
    // base de datos), este worker es PUSH-based: se suscribe a la Subscription
    // "notifications" y Azure Service Bus nos entrega los mensajes vía HandleMessageAsync
    // en cuanto llegan. No hay tabla ni columna ProcessedAt que consultar aquí -- el
    // "estado de qué ya se procesó" lo lleva Service Bus mismo (el lock + el ack).
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
                ?? throw new InvalidOperationException("Falta configurar ServiceBus:PaymentEventsTopic.");
            var subscriptionName = configuration["ServiceBus:NotificationsSubscription"]
                ?? throw new InvalidOperationException("Falta configurar ServiceBus:NotificationsSubscription.");

            // AutoCompleteMessages = false: queremos decidir explícitamente cuándo un
            // mensaje se completa (éxito) o se abandona (falla -> Service Bus lo vuelve a
            // entregar). MaxConcurrentCalls = 1 para que el demo sea determinista/ordenado.
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
                // El processor entrega mensajes en sus propios hilos internos; este método
                // solo necesita quedarse "vivo" mientras el host no pida detenerse.
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Esperado cuando el host cancela stoppingToken al apagar el worker.
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
                    ?? throw new JsonException($"El mensaje {args.Message.MessageId} no tiene un payload válido.");

                var request = new NotificationRequest
                {
                    OrderId = payload.OrderId,
                    TransactionId = payload.TransactionId,
                    Message = $"Tu pago para la orden #{payload.OrderId} fue procesado exitosamente (transacción {payload.TransactionId})."
                };

                await _dispatcher.DispatchAsync(request, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error procesando el mensaje {MessageId}; se abandona para reintento.",
                    args.Message.MessageId);
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Error en el ServiceBusProcessor ({ErrorSource}).", args.ErrorSource);
            return Task.CompletedTask;
        }
    }
}
