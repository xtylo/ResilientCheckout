using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Idempotency
{
    public interface IIdempotencyStore
    {
        Task<bool> TryReserveAsync(string key, int orderId, CancellationToken cancellationToken = default);

        // Libera una key reservada cuando la operación NUNCA se completó de verdad
        // (falla técnica del provider), para permitir un reintento legítimo con la misma key.
        // No se debe llamar cuando el resultado fue un rechazo de negocio (Succeed = false):
        // ese sí es un desenlace válido y la key debe seguir consumida.
        Task ReleaseAsync(string key, CancellationToken cancellationToken = default);
    }
}
