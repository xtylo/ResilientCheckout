using ResilientCheckout.Application.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Payments
{
    public interface IPaymentProvider
    {
        Task<PaymentResult> ChargeAsync(ChargeCardCommand command);
    }
}
