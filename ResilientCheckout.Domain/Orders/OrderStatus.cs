using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Orders
{
    internal enum OrderStatus
    {
        New,
        Processing,
        Paid
    }
}
