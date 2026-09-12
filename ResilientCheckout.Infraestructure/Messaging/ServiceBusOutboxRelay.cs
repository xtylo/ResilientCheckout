using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResilientCheckout.Application.Messaging;
using ResilientCheckout.Infraestructure.Persistence;

namespace ResilientCheckout.Infraestructure.Messaging
{
    // BackgroundService lives as a Singleton for the whole life of the app. That's why it
    // CANNOT receive AppDbContext (Scoped) directly in the constructor -- it needs to open
    // its own scope on every polling cycle to be able to use it. IEventPublisher IS safe
    // to inject directly here because it's also a Singleton.
    public class ServiceBusOutboxRelay : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(3);
        private const int BatchSize = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ServiceBusOutboxRelay> _logger;

        public ServiceBusOutboxRelay(
            IServiceScopeFactory scopeFactory,
            IEventPublisher eventPublisher,
            ILogger<ServiceBusOutboxRelay> logger)
        {
            _scopeFactory = scopeFactory;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PollingInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RelayPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A failure in one cycle (e.g. the database doesn't respond for a moment)
                    // must not bring down the whole BackgroundService -- it's logged and
                    // retried on the next tick.
                    _logger.LogError(ex, "Unexpected failure in the outbox relay cycle.");
                }
            }
        }

        private async Task RelayPendingMessagesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingMessages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (pendingMessages.Count == 0)
                return;

            foreach (var message in pendingMessages)
            {
                try
                {
                    await _eventPublisher.PublishAsync(message, cancellationToken);
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    _logger.LogWarning(ex,
                        "Could not publish OutboxMessage {MessageId} (attempt #{RetryCount}).",
                        message.Id, message.RetryCount);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox relay: {Processed}/{Total} messages published in this cycle.",
                pendingMessages.Count(m => m.ProcessedAt != null),
                pendingMessages.Count);
        }
    }
}
