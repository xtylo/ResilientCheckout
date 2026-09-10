using ResilientCheckout.Domain.Outbox;

namespace ResilientCheckout.Application.Messaging
{
    // Abstracción de "publicar un evento del outbox hacia afuera". Application/Domain
    // no saben que por debajo es Azure Service Bus — solo Infraestructure lo sabe.
    public interface IEventPublisher
    {
        Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    }
}
