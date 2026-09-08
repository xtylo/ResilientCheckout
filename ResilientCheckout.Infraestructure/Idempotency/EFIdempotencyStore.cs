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

        public async Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
        {
            var record = await _appDbContext.IdempotencyRecords
                .FirstOrDefaultAsync(ir => ir.Key == key, cancellationToken);

            if (record is null)
                return;

            _appDbContext.IdempotencyRecords.Remove(record);
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
