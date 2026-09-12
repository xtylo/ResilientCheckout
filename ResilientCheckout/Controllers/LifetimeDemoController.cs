using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Api.LifetimeDemo;

namespace ResilientCheckout.Api.Controllers
{
    [ApiController]
    [Route("api/lifetime-demo")]
    public class LifetimeDemoController : ControllerBase
    {
        // Las tres interfaces resuelven al MISMO OperationService.cs, cada una
        // registrada bajo un lifetime distinto (ver Program.cs). Pedir cada una DOS
        // VECES en la misma request -- vía dos parámetros [FromServices] -- es lo que
        // hace visible la diferencia:
        //   - Singleton: A y B son el MISMO Guid, siempre (toda la vida de la app).
        //   - Scoped: A y B son el MISMO Guid DENTRO de esta request, pero cambia en
        //     cada request nueva (un scope = un request en ASP.NET Core).
        //   - Transient: A y B son SIEMPRE distintos, incluso dentro de la misma request.
        // Llama a este endpoint varias veces y compara los valores entre llamadas.
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
                    MismaInstanciaEnEstaRequest = singletonA.OperationId == singletonB.OperationId
                },
                Scoped = new
                {
                    A = scopedA.OperationId,
                    B = scopedB.OperationId,
                    MismaInstanciaEnEstaRequest = scopedA.OperationId == scopedB.OperationId
                },
                Transient = new
                {
                    A = transientA.OperationId,
                    B = transientB.OperationId,
                    MismaInstanciaEnEstaRequest = transientA.OperationId == transientB.OperationId
                }
            });
        }
    }
}
