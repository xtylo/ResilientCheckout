using ResilientCheckout.Domain.Outbox;

namespace ResilientCheckout.Application.Messaging
{
    // Abstraction for "publish an outbox event to the outside world". Application/Domain
    // don't know that underneath it's Azure Service Bus -- only Infraestructure knows that.
    public interface IEventPublisher
    {
        Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    }
}
