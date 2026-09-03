using ResilientCheckout.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Payments
{
    public class PaymentResult : BaseEntity
    {

        public int OrderId { get; set; }

        public bool Succeed { get; set; }

        public Order Order { get; set; }

        public PaymentProvider Provider { get; set; }

        public string TransactionId { get; set; } = string.Empty;

    }
}
