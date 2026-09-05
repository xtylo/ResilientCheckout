using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Infraestructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ResilientCheckout.Infraestructure.Outbox
{
    public class EFOutboxWritter : IOutboxWritter
    {
        private readonly AppDbContext _appDbContext;

        public EFOutboxWritter(AppDbContext appDbContext) => _appDbContext = appDbContext;

        public void Add(EventType type, object payload)
        {
            _appDbContext.OutboxMessages.Add(new OutboxMessage
            {
                EventType = type,
                Payload = JsonSerializer.Serialize(payload)
            });
        }
    }
}
