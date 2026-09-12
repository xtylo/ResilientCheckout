using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications.Channels
{
    public class SmsChannel : INotificationChannel
    {
        private readonly ILogger<SmsChannel> _logger;

        public SmsChannel(ILogger<SmsChannel> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SMS] Order {OrderId}: {Message}", request.OrderId, request.Message);
            return Task.CompletedTask;
        }
    }
}
