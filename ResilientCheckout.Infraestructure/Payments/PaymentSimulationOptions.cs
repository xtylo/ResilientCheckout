using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Payments
{
    // Singleton: el estado debe sobrevivir entre requests para que puedas
    // "activar modo falla" con un request y luego probar el endpoint de charge en otro.
    public class PaymentSimulationOptions
    {
        public PaymentSimulationMode Mode { get; private set; } = PaymentSimulationMode.Success;
        private int _remainingFailures;

        public void SetTransientFailure(int failuresBeforeRecovering = 2)
        {
            Mode = PaymentSimulationMode.TransientFailure;
            _remainingFailures = failuresBeforeRecovering;
        }

        public void SetPersistentFailure() => Mode = PaymentSimulationMode.PersistentFailure;
        public void Reset() => Mode = PaymentSimulationMode.Success;

        public bool ShouldFail()
        {
            if (Mode == PaymentSimulationMode.PersistentFailure)
                return true;

            if (Mode == PaymentSimulationMode.TransientFailure)
            {
                if (_remainingFailures > 0) { _remainingFailures--; return true; }
                Mode = PaymentSimulationMode.Success; // se "recupera" solo tras N fallas
            }

            return false;
        }
    }
}
