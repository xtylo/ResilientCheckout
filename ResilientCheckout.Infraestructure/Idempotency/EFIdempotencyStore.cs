using Microsoft.EntityFrameworkCore;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Domain.Idempotency;
using ResilientCheckout.Infraestructure.Persistence;


namespace ResilientCheckout.Infraestructure.Idempotency
{
    public class EFIdempotencyStore : IIdempotencyStore
    {
        private readonly AppDbContext _appDbContext;

        public EFIdempotencyStore (AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<bool> TryReserveAsync(string key, int orderId, CancellationToken cancellationToken = default)
        {
            var record = new IdempotencyRecord
            {
                Key = key,
                OrderId = orderId
            };

            _appDbContext.IdempotencyRecords.Add(record);

            try
            {
                await _appDbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                // Otra request ya reservó esta key primero (violación del unique constraint sobre Key).
                _appDbContext.Entry(record).State = EntityState.Detached;
                return false;
            }
        }
    }
}
