using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications.Channels
{
    // Fake -- same as StripeFakeProvider, simulates sending without calling anything real.
    public class EmailChannel : INotificationChannel
    {
        private readonly ILogger<EmailChannel> _logger;

        public EmailChannel(ILogger<EmailChannel> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Email] Order {OrderId}: {Message}", request.OrderId, request.Message);
            return Task.CompletedTask;
        }
    }
}
