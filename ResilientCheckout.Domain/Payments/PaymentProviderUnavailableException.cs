using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Domain.Payments
{
    public class PaymentProviderUnavailableException : Exception
    {
        public PaymentProviderUnavailableException(string providerName)
            : base($"{providerName} payment provider is unavailable.") { }
    }
}
