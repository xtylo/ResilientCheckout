namespace ResilientCheckout.Notifications
{
    // Espejo del payload que ServiceBusPublisher serializa desde el outbox:
    // new { result.OrderId, result.TransactionId, result.Provider }.
    // Este worker es el único dueño de este "contrato de mensaje" -- si el payload del
    // publisher cambia, este es el archivo que hay que actualizar.
    internal class PaymentEventPayload
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}
