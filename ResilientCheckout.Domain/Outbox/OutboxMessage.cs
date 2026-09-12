using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Outbox
{
    public class OutboxMessage : BaseEntity
    {
        public EventType EventType { get; set; }
        public string Payload { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }

        public int RetryCount { get; set; } = 0;
    }
}
