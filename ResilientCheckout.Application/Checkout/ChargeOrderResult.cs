using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Application.Checkout
{
    public enum ChargeOrderOutcome
    {
        Success,
        AlreadyProcessing,
        ProviderUnavailable,
        Rejected
    }

    // Neutral result of the business operation -- knows nothing about HTTP, about
    // ActionResult, or about status codes. The controller (Api) is the only one that
    // translates each Outcome into an HTTP response; Application shouldn't know that
    // an HTTP protocol exists above it.
    public class ChargeOrderResult
    {
        public ChargeOrderOutcome Outcome { get; }
        public PaymentResult? PaymentResult { get; }
        public string? Message { get; }

        private ChargeOrderResult(ChargeOrderOutcome outcome, PaymentResult? paymentResult, string? message)
        {
            Outcome = outcome;
            PaymentResult = paymentResult;
            Message = message;
        }

        public static ChargeOrderResult Success(PaymentResult result) =>
            new(ChargeOrderOutcome.Success, result, null);

        public static ChargeOrderResult AlreadyProcessing() =>
            new(ChargeOrderOutcome.AlreadyProcessing, null,
                "This charge has already been processed or is in progress.");

        public static ChargeOrderResult ProviderUnavailable(string message) =>
            new(ChargeOrderOutcome.ProviderUnavailable, null, message);

        public static ChargeOrderResult Rejected(PaymentResult result) =>
            new(ChargeOrderOutcome.Rejected, result, null);
    }
}
