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
        // By default, System.Text.Json serializes enums as their underlying numeric
        // value (e.g. PaymentProvider.Stripe -> 0). That's fragile for a message that
        // gets stored and published outward (Service Bus, the billing-audit
        // Subscription): if you ever reorder or insert a value in the enum,
        // the meaning of already-stored/published events changes silently.
        // Serializing as a string makes the payload self-descriptive and stable.
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
