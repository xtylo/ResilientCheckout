namespace ResilientCheckout.Domain.Notifications
{
    // Strategy: each delivery channel (email, sms, push...) implements this. The
    // NotificationDispatcher doesn't know or care how many or which implementations
    // exist -- that's decided by the composition in the Notifications project's Program.cs.
    public interface INotificationChannel
    {
        Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    }
}
