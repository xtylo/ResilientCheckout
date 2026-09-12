namespace ResilientCheckout.Domain.Notifications
{
    // Plain, non-persisted DTO -- what a notification channel needs to send
    // the message. It lives in Domain (same as ChargeInstruction) because both the Api
    // and the separate Notifications process reference it, and neither of them
    // should depend on the other.
    public class NotificationRequest
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
