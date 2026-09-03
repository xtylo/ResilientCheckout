
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Application.Commands;
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
            [FromServices] IPaymentProvider paymentProvider)
        {

            if (orderId != command.OrderId)
                return BadRequest("OrderId in the route does not match OrderId in the request body.");

            var result = await paymentProvider.ChargeAsync(command.ToChargeInstruction());

            if (!result.Succeed)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
