using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Infraestructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResilientCheckout.Infraestructure.Outbox
{
    public class EFOutboxWritter : IOutboxWritter
    {
        // Por defecto, System.Text.Json serializa enums como su valor numérico
        // subyacente (ej. PaymentProvider.Stripe -> 0). Eso es frágil para un mensaje
        // que se guarda y se publica hacia afuera (Service Bus, la Subscription
        // billing-audit): si el día de mañana reordenas o insertas un valor en el enum,
        // el significado de los eventos ya guardados/publicados cambia en silencio.
        // Serializar como string hace que el payload sea auto-descriptivo y estable.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly AppDbContext _appDbContext;

        public EFOutboxWritter(AppDbContext appDbContext) => _appDbContext = appDbContext;

        public void Add(EventType type, object payload)
        {
            _appDbContext.OutboxMessages.Add(new OutboxMessage
            {
                EventType = type,
                Payload = JsonSerializer.Serialize(payload, SerializerOptions)
            });
        }
    }
}
