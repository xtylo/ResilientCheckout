namespace ResilientCheckout.Domain.Notifications
{
    // DTO plano, no persistido -- lo que un canal de notificación necesita para mandar
    // el mensaje. Vive en Domain (igual que ChargeInstruction) porque tanto la Api
    // como el proceso separado de Notifications lo referencian, y ninguno de los dos
    // debe depender del otro.
    public class NotificationRequest
    {
        public int OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
