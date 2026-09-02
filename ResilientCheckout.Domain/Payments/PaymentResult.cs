using ResilientCheckout.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Payments
{
    internal class PaymentResult : BaseEntity
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        public bool? IsSuccessful { get; set; }


        public Order Order { get; set; }
    }
}
