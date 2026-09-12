using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResilientCheckout.Infraestructure.Idempotency;
using ResilientCheckout.Infraestructure.Persistence;
using Xunit;

namespace ResilientCheckout.Tests.Idempotency
{
    // FAKE: this isn't the real database (SQL Server/SQLite on disk) -- it's a
    // functional, lightweight implementation (in-memory SQLite) that behaves like the
    // real thing: same relational engine, same unique constraint on the Key column. The
    // class under test (EFIdempotencyStore) is NOT touched or replaced -- it runs exactly
    // as it does in production, just against a Fake of its only external dependency (the DB).
    public class EFIdempotencyStoreFakeTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly List<AppDbContext> _createdContexts = new();

        public EFIdempotencyStoreFakeTests()
        {
            // The connection must stay open for the whole test -- SQLite
            // ":memory:" wipes the database as soon as the last connection to it closes.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var schemaContext = new AppDbContext(_options);
            schemaContext.Database.EnsureCreated();
        }

        // Each call creates its OWN AppDbContext (its own change tracker) over the
        // SAME connection/database -- this faithfully mirrors what happens in production:
        // every HTTP request gets a fresh Scoped AppDbContext, never the same one another
        // request got. Reusing a single DbContext across "two requests" would hide the
        // real bug and instead trigger an InvalidOperationException from identity
        // resolution in the change tracker -- not the business exception (DbUpdateException
        // from the unique constraint) that EFIdempotencyStore knows how to handle.
        private EFIdempotencyStore CreateStore()
        {
            var dbContext = new AppDbContext(_options);
            _createdContexts.Add(dbContext);
            return new EFIdempotencyStore(dbContext);
        }

        [Fact]
        public async Task TryReserveAsync_WhenKeyIsNew_ReturnsTrue()
        {
            var store = CreateStore();

            var reserved = await store.TryReserveAsync("key-1", orderId: 1);

            Assert.True(reserved);
        }

        [Fact]
        public async Task TryReserveAsync_WhenKeyAlreadyReserved_ReturnsFalse()
        {
            var firstRequestStore = CreateStore();
            await firstRequestStore.TryReserveAsync("key-1", orderId: 1);

            // A concurrent "second request" using the SAME Idempotency-Key: it gets its
            // own AppDbContext (Scoped) -- that's why it's a new store, not the same one as above.
            var secondRequestStore = CreateStore();
            var secondAttempt = await secondRequestStore.TryReserveAsync("key-1", orderId: 1);

            Assert.False(secondAttempt);
        }

        [Fact]
        public async Task ReleaseAsync_AfterReserve_AllowsReservingTheSameKeyAgain()
        {
            var firstRequestStore = CreateStore();
            await firstRequestStore.TryReserveAsync("key-1", orderId: 1);
            await firstRequestStore.ReleaseAsync("key-1");

            // A later request (after the release) also gets its own DbContext.
            var laterRequestStore = CreateStore();
            var reservedAgain = await laterRequestStore.TryReserveAsync("key-1", orderId: 1);

            Assert.True(reservedAgain);
        }

        public void Dispose()
        {
            foreach (var context in _createdContexts)
                context.Dispose();

            _connection.Dispose();
        }
    }
}
