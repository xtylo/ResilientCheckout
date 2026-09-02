using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Orders
{
    internal class Order : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.New;

    }
}
