using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> TryReserveAsync(string key, int orderId, CancellationToken cancellationToken = default);

        // Releases a reserved key when the operation NEVER truly completed
        // (a technical failure of the provider), to allow a legitimate retry with the same key.
        // This must not be called when the outcome was a business rejection (Succeed = false):
        // that is a valid outcome and the key must stay consumed.
        Task ReleaseAsync(string key, CancellationToken cancellationToken = default);
    }
}
