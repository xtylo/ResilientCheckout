using Polly;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Infraestructure.Resilience
{
    // Decorator: envuelve a un IPaymentProvider "real" (o fake) con el pipeline de Polly.
    // Ni el provider interno ni el controller se enteran de que existe resiliencia aquí.
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
            return await _pipeline.ExecuteAsync(
                async cancellationToken => await _innerProvider.ChargeAsync(chargeInstruction));
        }
    }
}
