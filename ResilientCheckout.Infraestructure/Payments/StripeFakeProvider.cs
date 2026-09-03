using ResilientCheckout.Application.Commands;
using ResilientCheckout.Domain.Payments;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Payments
{
    public class StripeFakeProvider : IPaymentProvider
    {
        public Task<PaymentResult> ChargeAsync(ChargeCardCommand command)
        {
            return Task.FromResult(new PaymentResult
            {
                Succeed = true,
                OrderId = command.OrderId,
                Provider = PaymentProvider.Stripe,
                TransactionId = Guid.NewGuid().ToString()
            });
        }
    }
}
