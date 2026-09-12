using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Api.LifetimeDemo;

namespace ResilientCheckout.Api.Controllers
{
    [ApiController]
    [Route("api/lifetime-demo")]
    public class LifetimeDemoController : ControllerBase
    {
        // All three interfaces resolve to the SAME OperationService.cs, each one
        // registered under a different lifetime (see Program.cs). Requesting each one TWICE
        // within the same request -- via two [FromServices] parameters -- is what
        // makes the difference visible:
        //   - Singleton: A and B are the SAME Guid, always (for the whole life of the app).
        //   - Scoped: A and B are the SAME Guid WITHIN this request, but it changes on
        //     every new request (one scope = one request in ASP.NET Core).
        //   - Transient: A and B are ALWAYS different, even within the same request.
        // Call this endpoint several times and compare the values across calls.
        [HttpGet]
        public IActionResult Get(
            [FromServices] IOperationSingleton singletonA,
            [FromServices] IOperationSingleton singletonB,
            [FromServices] IOperationScoped scopedA,
            [FromServices] IOperationScoped scopedB,
            [FromServices] IOperationTransient transientA,
            [FromServices] IOperationTransient transientB)
        {
            return Ok(new
            {
                Singleton = new
                {
                    A = singletonA.OperationId,
                    B = singletonB.OperationId,
                    SameInstanceInThisRequest = singletonA.OperationId == singletonB.OperationId
                },
                Scoped = new
                {
                    A = scopedA.OperationId,
                    B = scopedB.OperationId,
                    SameInstanceInThisRequest = scopedA.OperationId == scopedB.OperationId
                },
                Transient = new
                {
                    A = transientA.OperationId,
                    B = transientB.OperationId,
                    SameInstanceInThisRequest = transientA.OperationId == transientB.OperationId
                }
            });
        }
    }
}
