using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Infraestructure.Payments;

namespace ResilientCheckout.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimulationController : ControllerBase
    {
        [HttpPost("{mode}")]
        public IActionResult SetSimulationMode(PaymentSimulationMode mode, [FromServices] PaymentSimulationOptions simulationOptions)
        {
            if (mode == PaymentSimulationMode.Success)
            {
                simulationOptions.Reset();
                return Ok(new { message = $"Simulation mode set to {mode}" });
            }
            else if (mode == PaymentSimulationMode.PersistentFailure)
            {
                simulationOptions.SetPersistentFailure();
                return Ok(new { message = $"Simulation mode set to {mode}" });
            }else if(mode == PaymentSimulationMode.TransientFailure)
            {
                simulationOptions.SetTransientFailure();
                return Ok(new { message = $"Simulation mode set to {mode}" });
            }
            else
            {
                return BadRequest(new { message = "Invalid simulation mode. Use 'success', 'transientfailure' or 'persistentfailure'." });
            }
        }

        // Hot-switches which concrete IPaymentProvider the Polly decorator wraps
        // (see Program.cs) -- without restarting the app and without touching the rest of the
        // checkout flow. This is what makes it tangible that IPaymentProvider is a real
        // Strategy: two interchangeable implementations behind the same abstraction,
        // not just an interface with a single live implementation.
        [HttpPost("provider/{provider}")]
        public IActionResult SetPaymentProvider(PaymentProviderKind provider, [FromServices] PaymentProviderSelection selection)
        {
            selection.SetProvider(provider);
            return Ok(new { message = $"Payment provider set to {provider}" });
        }
    }
}
