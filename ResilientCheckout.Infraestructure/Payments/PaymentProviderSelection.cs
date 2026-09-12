using Microsoft.Extensions.Configuration;
using System;

namespace ResilientCheckout.Infraestructure.Payments
{
    // Singleton -- mismo criterio que PaymentSimulationOptions: el proveedor activo
    // debe sobrevivir entre requests para poder cambiarlo en caliente (vía
    // POST /api/simulation/provider/{provider}) sin reiniciar la app. Arranca con el
    // valor de appsettings ("Payments:Provider"), o Stripe si no se configuró nada.
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
