using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Application.Checkout
{
    // All the business orchestration that used to live in CheckoutController: reserving
    // idempotency, charging, deciding what to do with each outcome (technical vs.
    // business), writing the outbox, and confirming. The controller is left as a simple
    // HTTP <-> Command translator; this class is the one that truly knows "how an order
    // gets charged" and can be tested without touching ASP.NET Core -- it doesn't even
    // need a fake HttpContext.
    public class ChargeOrderHandler : IChargeOrderHandler
    {
        private readonly IPaymentProvider _paymentProvider;
        private readonly IIdempotencyStore _idempotencyStore;
        private readonly IOutboxWritter _outboxWriter;
        private readonly IUnitOfWork _unitOfWork;

        public ChargeOrderHandler(
            IPaymentProvider paymentProvider,
            IIdempotencyStore idempotencyStore,
            IOutboxWritter outboxWriter,
            IUnitOfWork unitOfWork)
        {
            _paymentProvider = paymentProvider;
            _idempotencyStore = idempotencyStore;
            _outboxWriter = outboxWriter;
            _unitOfWork = unitOfWork;
        }

        public async Task<ChargeOrderResult> HandleAsync(
            ChargeCardCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var reserved = await _idempotencyStore.TryReserveAsync(idempotencyKey, command.OrderId, cancellationToken);
            if (!reserved)
                return ChargeOrderResult.AlreadyProcessing();

            PaymentResult result;
            try
            {
                result = await _paymentProvider.ChargeAsync(command.ToChargeInstruction());
            }
            catch (PaymentProviderUnavailableException)
            {
                // Covers both "retries were exhausted" and "the circuit was already
                // open" -- ResilientPaymentProvider translates both cases into this single
                // Domain exception type (see its comment). Either way, the operation
                // never truly completed, so the key is released to allow a legitimate
                // retry with the same Idempotency-Key.
                await _idempotencyStore.ReleaseAsync(idempotencyKey, cancellationToken);
                return ChargeOrderResult.ProviderUnavailable(
                    "Could not process the charge right now. Please try again later.");
            }

            if (!result.Succeed)
                return ChargeOrderResult.Rejected(result);

            _outboxWriter.Add(EventType.PaymentSucceeded, new { result.OrderId, result.TransactionId, result.Provider });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ChargeOrderResult.Success(result);
        }
    }
}
