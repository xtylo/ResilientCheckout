using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications.Channels
{
    public class PushChannel : INotificationChannel
    {
        private readonly ILogger<PushChannel> _logger;

        public PushChannel(ILogger<PushChannel> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Push] Orden {OrderId}: {Message}", request.OrderId, request.Message);
            return Task.CompletedTask;
        }
    }
}
