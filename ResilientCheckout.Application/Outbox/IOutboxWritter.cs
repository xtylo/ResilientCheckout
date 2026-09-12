using ResilientCheckout.Domain.Outbox;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Outbox
{
    public interface IOutboxWritter
    {
        void Add(EventType type, object payload);

    }
}
