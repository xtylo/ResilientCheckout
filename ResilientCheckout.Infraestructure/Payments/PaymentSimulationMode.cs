using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Payments
{
    public enum PaymentSimulationMode
    {
        Success,
        TransientFailure,
        PersistentFailure
    }
}
