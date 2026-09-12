using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ResilientCheckout.Domain.Idempotency
{
    public class IdempotencyRecord 
    {
        public string Key { get; set; }

        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
