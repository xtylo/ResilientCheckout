using ResilientCheckout.Domain.Payments;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Payments
{
    public class PaypalFakeProvider : IPaymentProvider
    {
        private readonly PaymentSimulationOptions _simulation;

        public PaypalFakeProvider(PaymentSimulationOptions simulation) {
            _simulation = simulation;
        }

        public Task<PaymentResult> ChargeAsync(ChargeInstruction chargeInstruction)
        {
            if (_simulation.ShouldFail())
                return Task.FromException<PaymentResult>(new PaymentProviderUnavailableException("Paypal"));

            return Task.FromResult(new PaymentResult
            {
                Succeed = true,
                OrderId = chargeInstruction.OrderId,
                Provider = PaymentProvider.Paypal,
                TransactionId = Guid.NewGuid().ToString()
            });
        }
    }
}
