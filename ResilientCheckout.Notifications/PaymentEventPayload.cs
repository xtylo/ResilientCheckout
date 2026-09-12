namespace ResilientCheckout.Notifications
{
    // Mirror of the payload ServiceBusPublisher serializes from the outbox:
    // new { result.OrderId, result.TransactionId, result.Provider }.
    // This worker is the sole owner of this "message contract" -- if the publisher's
    // payload changes, this is the file that needs to be updated.
    internal class PaymentEventPayload
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}
