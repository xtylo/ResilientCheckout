namespace ResilientCheckout.Domain.Notifications
{
    // Strategy: cada medio de envío (email, sms, push...) implementa esto. El
    // NotificationDispatcher no sabe ni le importa cuántas ni cuáles implementaciones
    // existen -- eso lo decide la composición en Program.cs del proyecto Notifications.
    public interface INotificationChannel
    {
        Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    }
}
