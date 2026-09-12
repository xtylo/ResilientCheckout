using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResilientCheckout.Infraestructure.Idempotency;
using ResilientCheckout.Infraestructure.Persistence;
using Xunit;

namespace ResilientCheckout.Tests.Idempotency
{
    // FAKE: no es la base real (SQL Server/SQLite en disco) -- es una implementación
    // funcional y liviana (SQLite en memoria) que se comporta como la real: mismo
    // motor relacional, misma restricción unique sobre la columna Key. La clase bajo
    // prueba (EFIdempotencyStore) NO se toca ni se reemplaza -- corre tal cual corre
    // en producción, solo que contra un Fake de su única dependencia externa (la BD).
    public class EFIdempotencyStoreFakeTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly List<AppDbContext> _createdContexts = new();

        public EFIdempotencyStoreFakeTests()
        {
            // La conexión debe permanecer abierta durante todo el test -- SQLite
            // ":memory:" borra la base en cuanto se cierra la última conexión sobre ella.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var schemaContext = new AppDbContext(_options);
            schemaContext.Database.EnsureCreated();
        }

        // Cada llamada crea su PROPIO AppDbContext (su propio change tracker) sobre la
        // MISMA conexión/base -- así se modela fielmente lo que pasa en producción:
        // cada request HTTP recibe un AppDbContext Scoped fresco, nunca el mismo que
        // otra request. Reusar un solo DbContext entre "dos requests" escondería el bug
        // real y dispararía en su lugar un InvalidOperationException de identity
        // resolution en el change tracker -- no la excepción de negocio (DbUpdateException
        // por el unique constraint) que EFIdempotencyStore sabe manejar.
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

            // "Segunda request" concurrente usando la MISMA Idempotency-Key: recibe su
            // propio AppDbContext (Scoped) -- por eso es un store nuevo, no el mismo de arriba.
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

            // Una request posterior (tras el release) también recibe su propio DbContext.
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
