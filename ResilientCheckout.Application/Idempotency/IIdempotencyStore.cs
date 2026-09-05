using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> TryReserveAsync(string key, int orderId, CancellationToken cancellationToken = default);
    }
}
