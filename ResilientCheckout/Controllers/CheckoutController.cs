
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Application.Checkout;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : Controller
    {

        // The controller no longer orchestrates any business logic -- it only validates the
        // shape of the request (header present, orderId consistent between route and body),
        // delegates to the handler, and translates its ChargeOrderResult into an HTTP
        // response. All the real logic (idempotency, charging, outbox) lives in
        // ChargeOrderHandler (Application), where it can be tested without depending on ASP.NET Core.
        [HttpPost("{orderId}/charge")]
        public async Task<ActionResult<PaymentResult>> Charge(
            int orderId,
            [FromBody] ChargeCardCommand command,
            [FromServices] IChargeOrderHandler handler)
        {

            var idempotencyKey = Request.Headers["Idempotency-Key"];
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest("Missing idempotency key");

            if (orderId != command.OrderId)
                return BadRequest("OrderId in the route does not match OrderId in the request body.");

            var result = await handler.HandleAsync(command, idempotencyKey!);

            return result.Outcome switch
            {
                ChargeOrderOutcome.Success => Ok(result.PaymentResult),
                ChargeOrderOutcome.AlreadyProcessing => Conflict(result.Message),
                ChargeOrderOutcome.ProviderUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result.Message),
                ChargeOrderOutcome.Rejected => BadRequest(result.PaymentResult),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}
