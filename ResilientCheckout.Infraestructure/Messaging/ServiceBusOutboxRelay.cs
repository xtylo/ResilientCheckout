using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResilientCheckout.Application.Messaging;
using ResilientCheckout.Infraestructure.Persistence;

namespace ResilientCheckout.Infraestructure.Messaging
{
    // BackgroundService vive como Singleton durante toda la vida de la app. Por eso NO
    // puede recibir AppDbContext (Scoped) directo en el constructor -- necesita abrir su
    // propio scope en cada ciclo de polling para poder usarlo. IEventPublisher sí es
    // seguro de inyectar directo aquí porque también es Singleton.
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
                    // Un fallo en un ciclo (ej. la base no responde un momento) no debe
                    // tumbar el BackgroundService entero -- se registra y se reintenta
                    // en el siguiente tick.
                    _logger.LogError(ex, "Fallo inesperado en el ciclo del outbox relay.");
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
                        "No se pudo publicar el OutboxMessage {MessageId} (intento #{RetryCount}).",
                        message.Id, message.RetryCount);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox relay: {Processed}/{Total} mensajes publicados en este ciclo.",
                pendingMessages.Count(m => m.ProcessedAt != null),
                pendingMessages.Count);
        }
    }
}
