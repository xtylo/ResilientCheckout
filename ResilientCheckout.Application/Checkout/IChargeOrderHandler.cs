using ResilientCheckout.Application.Commands;

namespace ResilientCheckout.Application.Checkout
{
    public interface IChargeOrderHandler
    {
        Task<ChargeOrderResult> HandleAsync(
            ChargeCardCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken = default);
    }
}
