using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Orders
{
    public enum OrderStatus
    {
        New,
        Processing,
        Paid
    }
}
