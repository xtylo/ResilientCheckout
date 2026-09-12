using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Infraestructure.Payments
{
    // Singleton: the state must survive across requests so you can
    // "turn on failure mode" with one request and then test the charge endpoint in another.
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
                Mode = PaymentSimulationMode.Success; // "recovers" on its own after N failures
            }

            return false;
        }
    }
}
