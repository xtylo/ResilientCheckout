
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : Controller
    {

        [HttpPost("{orderId}/charge")]
        public async Task<ActionResult<PaymentResult>> Charge(
            int orderId,
            [FromBody] ChargeCardCommand command,
            [FromServices] IPaymentProvider paymentProvider,
            [FromServices] IIdempotencyStore idempotencyStore,
            [FromServices] IOutboxWritter outboxWriter,
            [FromServices] IUnitOfWork unitOfWork)
        {

            var idempotencyKey = Request.Headers["Idempotency-Key"];
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest("Missing idempotency key");

            if (orderId != command.OrderId)
                return BadRequest("OrderId in the route does not match OrderId in the request body.");

            var reserved = await idempotencyStore.TryReserveAsync(idempotencyKey, command.OrderId);
            if (!reserved)
                return Conflict("This charge has already been processed or is in progress.");

            var result = await paymentProvider.ChargeAsync(command.ToChargeInstruction());

            if (!result.Succeed)
                return BadRequest(result);

            outboxWriter.Add(EventType.PaymentSucceeded, new { result.OrderId, result.TransactionId, result.Provider });

            await unitOfWork.SaveChangesAsync();

            return Ok(result);
        }
    }
}
