using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using ResilientCheckout.Application.Messaging;
using ResilientCheckout.Domain.Outbox;

namespace ResilientCheckout.Infraestructure.Messaging
{
    // Publishes to the "payment-events" Topic. Registered as a Singleton (see Program.cs):
    // the ServiceBusSender is meant to live for the whole life of the app, not be created
    // per request -- same as the ServiceBusClient it comes from.
    public class ServiceBusPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusPublisher(ServiceBusClient client, IConfiguration configuration)
        {
            var topicName = configuration["ServiceBus:PaymentEventsTopic"]
                ?? throw new InvalidOperationException("ServiceBus:PaymentEventsTopic is not configured.");

            _sender = client.CreateSender(topicName);
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            var sbMessage = new ServiceBusMessage(message.Payload)
            {
                MessageId = message.Id.ToString(),
                Subject = message.EventType.ToString(),
                ContentType = "application/json"
            };

            await _sender.SendMessageAsync(sbMessage, cancellationToken);
        }

        public ValueTask DisposeAsync() => _sender.DisposeAsync();
    }
}
