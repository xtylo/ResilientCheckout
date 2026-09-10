using Microsoft.Extensions.Logging;
using ResilientCheckout.Domain.Notifications;

namespace ResilientCheckout.Notifications
{
    // Se inyecta IEnumerable<INotificationChannel> -- el contenedor de DI junta TODAS
    // las implementaciones registradas para esa interfaz (Email, Sms, Push) en una sola
    // colección. Así el dispatcher no conoce ninguna implementación concreta y agregar
    // un canal nuevo mañana es solo una línea más de registro en Program.cs.
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
                    // Un canal caído (ej. el proveedor de SMS no responde) no debe tumbar
                    // a los demás -- cada canal se intenta de forma independiente.
                    _logger.LogWarning(ex, "El canal {Channel} falló al notificar la orden {OrderId}.",
                        channel.GetType().Name, request.OrderId);
                }
            }
        }
    }
}
