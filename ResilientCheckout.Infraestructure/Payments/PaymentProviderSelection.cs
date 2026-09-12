using Microsoft.Extensions.Configuration;
using System;

namespace ResilientCheckout.Infraestructure.Payments
{
    // Singleton -- same reasoning as PaymentSimulationOptions: the active provider
    // must survive across requests so it can be hot-switched (via
    // POST /api/simulation/provider/{provider}) without restarting the app. Starts with the
    // value from appsettings ("Payments:Provider"), or Stripe if nothing was configured.
    public class PaymentProviderSelection
    {
        public PaymentProviderKind Current { get; private set; }

        public PaymentProviderSelection(IConfiguration configuration)
        {
            var configured = configuration["Payments:Provider"];
            Current = Enum.TryParse<PaymentProviderKind>(configured, ignoreCase: true, out var parsed)
                ? parsed
                : PaymentProviderKind.Stripe;
        }

        public void SetProvider(PaymentProviderKind provider) => Current = provider;
    }
}
