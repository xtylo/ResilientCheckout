using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Api.Controllers;
using ResilientCheckout.Application.Checkout;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Domain.Payments;
using Xunit;

namespace ResilientCheckout.Tests.Checkout
{
    // The controller, now thin after the Command+Handler refactor, only has three
    // responsibilities: validating the shape of the request (header present, orderId
    // consistent between route and body), delegating to the handler, and translating its
    // ChargeOrderResult into an HTTP status code. A StubChargeOrderHandler that returns
    // a canned result -- a Stub again, this time at the controller's boundary
    // instead of the payment provider's -- lets us test exactly that,
    // without touching idempotency, EF Core, or Polly.
    public class CheckoutControllerTests
    {
        private class StubChargeOrderHandler : IChargeOrderHandler
        {
            private readonly ChargeOrderResult _result;

            public StubChargeOrderHandler(ChargeOrderResult result) => _result = result;

            public Task<ChargeOrderResult> HandleAsync(
                ChargeCardCommand command, string idempotencyKey, CancellationToken cancellationToken = default)
                => Task.FromResult(_result);
        }

        private static CheckoutController CreateController(string? idempotencyKey)
        {
            var controller = new CheckoutController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            if (idempotencyKey is not null)
                controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;

            return controller;
        }

        [Fact]
        public async Task Charge_WhenIdempotencyKeyHeaderIsMissing_ReturnsBadRequest()
        {
            var controller = CreateController(idempotencyKey: null);
            // The handler should never be called -- the controller must short-circuit before that.
            var handler = new StubChargeOrderHandler(ChargeOrderResult.AlreadyProcessing());

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 1 }, handler: handler);

            Assert.IsType<BadRequestObjectResult>(response.Result);
        }

        [Fact]
        public async Task Charge_WhenRouteOrderIdDoesNotMatchBody_ReturnsBadRequest()
        {
            var controller = CreateController("some-key");
            var handler = new StubChargeOrderHandler(ChargeOrderResult.AlreadyProcessing());

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 2 }, handler: handler);

            Assert.IsType<BadRequestObjectResult>(response.Result);
        }

        [Fact]
        public async Task Charge_WhenHandlerReturnsSuccess_ReturnsOk()
        {
            var controller = CreateController("some-key");
            var paymentResult = new PaymentResult
            {
                OrderId = 1,
                Succeed = true,
                Provider = PaymentProvider.Stripe,
                TransactionId = "tx"
            };
            var handler = new StubChargeOrderHandler(ChargeOrderResult.Success(paymentResult));

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 1 }, handler: handler);

            Assert.IsType<OkObjectResult>(response.Result);
        }

        [Fact]
        public async Task Charge_WhenHandlerReturnsAlreadyProcessing_ReturnsConflict()
        {
            var controller = CreateController("some-key");
            var handler = new StubChargeOrderHandler(ChargeOrderResult.AlreadyProcessing());

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 1 }, handler: handler);

            Assert.IsType<ConflictObjectResult>(response.Result);
        }

        [Fact]
        public async Task Charge_WhenHandlerReturnsProviderUnavailable_ReturnsServiceUnavailable()
        {
            var controller = CreateController("some-key");
            var handler = new StubChargeOrderHandler(ChargeOrderResult.ProviderUnavailable("not available"));

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 1 }, handler: handler);

            var objectResult = Assert.IsType<ObjectResult>(response.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        }

        [Fact]
        public async Task Charge_WhenHandlerReturnsRejected_ReturnsBadRequest()
        {
            var controller = CreateController("some-key");
            var paymentResult = new PaymentResult
            {
                OrderId = 1,
                Succeed = false,
                Provider = PaymentProvider.Stripe
            };
            var handler = new StubChargeOrderHandler(ChargeOrderResult.Rejected(paymentResult));

            var response = await controller.Charge(
                orderId: 1, command: new ChargeCardCommand { OrderId = 1 }, handler: handler);

            Assert.IsType<BadRequestObjectResult>(response.Result);
        }
    }
}
