using Microsoft.EntityFrameworkCore;
using ResilientCheckout.Domain.Idempotency;
using ResilientCheckout.Domain.Outbox;


namespace ResilientCheckout.Infraestructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdempotencyRecord>()
                .HasKey(i => i.Key);
            
        }
    }
}
