using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using ResilientCheckout.Application.Messaging;
using ResilientCheckout.Domain.Outbox;

namespace ResilientCheckout.Infraestructure.Messaging
{
    // Publica al Topic "payment-events". Se registra como Singleton (ver Program.cs):
    // el ServiceBusSender está pensado para vivir toda la vida de la app, no crearse
    // por request — igual que el ServiceBusClient del que sale.
    public class ServiceBusPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;

        public ServiceBusPublisher(ServiceBusClient client, IConfiguration configuration)
        {
            var topicName = configuration["ServiceBus:PaymentEventsTopic"]
                ?? throw new InvalidOperationException("Falta configurar ServiceBus:PaymentEventsTopic.");

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
