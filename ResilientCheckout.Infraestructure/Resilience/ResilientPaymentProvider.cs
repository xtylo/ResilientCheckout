using Polly;
using Polly.CircuitBreaker;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Infraestructure.Resilience
{
    // Decorator: wraps a "real" (or fake) IPaymentProvider with the Polly pipeline.
    // Neither the inner provider nor the rest of the app knows resilience is happening
    // here -- they don't even know Polly exists. BrokenCircuitException is a Polly
    // implementation detail (thrown when the circuit is open and the call is cut short
    // before even attempting the inner provider); it's translated right here into
    // PaymentProviderUnavailableException -- a Domain type -- so that nothing outside
    // Infraestructura needs to know about or reference Polly. Application/Api now only
    // need to catch ONE exception type, regardless of whether the cause was
    // exhausted retries or an already-open circuit.
    public class ResilientPaymentProvider : IPaymentProvider
    {
        private readonly IPaymentProvider _innerProvider;
        private readonly ResiliencePipeline<PaymentResult> _pipeline;

        public ResilientPaymentProvider(IPaymentProvider innerProvider, ResiliencePipeline<PaymentResult> pipeline)
        {
            _innerProvider = innerProvider;
            _pipeline = pipeline;
        }

        public async Task<PaymentResult> ChargeAsync(ChargeInstruction chargeInstruction)
        {
            try
            {
                return await _pipeline.ExecuteAsync(
                    async cancellationToken => await _innerProvider.ChargeAsync(chargeInstruction));
            }
            catch (BrokenCircuitException)
            {
                throw new PaymentProviderUnavailableException(_innerProvider.GetType().Name);
            }
        }
    }
}
