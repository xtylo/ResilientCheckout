using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications.Channels
{
    // Fake -- igual que StripeFakeProvider, simula el envío sin llamar a nada real.
    public class EmailChannel : INotificationChannel
    {
        private readonly ILogger<EmailChannel> _logger;

        public EmailChannel(ILogger<EmailChannel> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Email] Orden {OrderId}: {Message}", request.OrderId, request.Message);
            return Task.CompletedTask;
        }
    }
}
