using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications
{
    // IEnumerable<INotificationChannel> is injected -- the DI container gathers ALL
    // implementations registered for that interface (Email, Sms, Push) into a single
    // collection. That way the dispatcher doesn't know any concrete implementation and
    // adding a new channel tomorrow is just one more registration line in Program.cs.
    public class NotificationDispatcher
    {
        private readonly IEnumerable<INotificationChannel> _channels;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(IEnumerable<INotificationChannel> channels, ILogger<NotificationDispatcher> logger)
        {
            _channels = channels;
            _logger = logger;
        }

        public async Task DispatchAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            foreach (var channel in _channels)
            {
                try
                {
                    await channel.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    // A channel that's down (e.g. the SMS provider doesn't respond) must not bring
                    // down the others -- each channel is attempted independently.
                    _logger.LogWarning(ex, "Channel {Channel} failed to notify order {OrderId}.",
                        channel.GetType().Name, request.OrderId);
                }
            }
        }
    }
}
