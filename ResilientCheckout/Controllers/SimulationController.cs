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
                return BadRequest(new { message = "Invalid simulation mode. Use 'success' or 'failure'." });
            }
        }

        // Cambia en caliente cuál IPaymentProvider concreto envuelve el decorator de
        // Polly (ver Program.cs) -- sin reiniciar la app y sin tocar el resto del flujo
        // de checkout. Esto es lo que hace tangible que IPaymentProvider es un Strategy
        // real: dos implementaciones intercambiables detrás de la misma abstracción,
        // no solo una interfaz con una única implementación viva.
        [HttpPost("provider/{provider}")]
        public IActionResult SetPaymentProvider(PaymentProviderKind provider, [FromServices] PaymentProviderSelection selection)
        {
            selection.SetProvider(provider);
            return Ok(new { message = $"Payment provider set to {provider}" });
        }
    }
}
