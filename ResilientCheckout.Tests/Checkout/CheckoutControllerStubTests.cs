using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResilientCheckout.Api.Controllers;
using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Domain.Payments;
using Xunit;

namespace ResilientCheckout.Tests.Checkout
{
    // STUB: StubPaymentProvider siempre regresa la MISMA respuesta enlatada sin
    // importar qué se le pida, y nadie verifica cómo ni cuántas veces se le llamó --
    // esa es la diferencia clave contra un Mock. Sirve para "quitar del camino" al
    // proveedor de pago cuando lo que de verdad se quiere probar es el resto del flujo
    // del controller (idempotencia, mapeo de la respuesta).
    public class CheckoutControllerStubTests
    {
        private class StubPaymentProvider : IPaymentProvider
        {
            public Task<PaymentResult> ChargeAsync(ChargeInstruction chargeInstruction)
            {
                var canned = new PaymentResult
                {
                    OrderId = chargeInstruction.OrderId,
                    Succeed = true,
                    Provider = PaymentProvider.Stripe,
                    TransactionId = "stub-transaction-id"
                };
                return Task.FromResult(canned);
            }
        }

        // Fakes mínimos -- solo para que el resto del flujo complete sin tocar EF Core
        // ni Service Bus. No son el foco de este archivo (ese es EFIdempotencyStoreFakeTests).
        private class FakeIdempotencyStore : IIdempotencyStore
        {
            private readonly HashSet<string> _reservedKeys = new();

            public Task<bool> TryReserveAsync(string key, int orderId, CancellationToken cancellationToken = default)
                => Task.FromResult(_reservedKeys.Add(key));

            public Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
            {
                _reservedKeys.Remove(key);
                return Task.CompletedTask;
            }
        }

        private class FakeOutboxWritter : IOutboxWritter
        {
            public List<(EventType Type, object Payload)> AddedMessages { get; } = new();

            public void Add(EventType type, object payload) => AddedMessages.Add((type, payload));
        }

        private class FakeUnitOfWork : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(1);
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
        public async Task Charge_WhenPaymentProviderSucceeds_ReturnsOkWithPaymentResult()
        {
            var controller = CreateController("test-key-1");
            var command = new ChargeCardCommand { OrderId = 1 };

            var response = await controller.Charge(
                orderId: 1,
                command: command,
                paymentProvider: new StubPaymentProvider(),
                idempotencyStore: new FakeIdempotencyStore(),
                outboxWriter: new FakeOutboxWritter(),
                unitOfWork: new FakeUnitOfWork());

            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var paymentResult = Assert.IsType<PaymentResult>(okResult.Value);
            Assert.True(paymentResult.Succeed);
            Assert.Equal("stub-transaction-id", paymentResult.TransactionId);
        }

        [Fact]
        public async Task Charge_WhenIdempotencyKeyHeaderIsMissing_ReturnsBadRequest()
        {
            // Sin Idempotency-Key el controller debe cortar antes de tocar al payment
            // provider -- el Stub está ahí, pero no debería ni usarse.
            var controller = CreateController(idempotencyKey: null);
            var command = new ChargeCardCommand { OrderId = 1 };

            var response = await controller.Charge(
                orderId: 1,
                command: command,
                paymentProvider: new StubPaymentProvider(),
                idempotencyStore: new FakeIdempotencyStore(),
                outboxWriter: new FakeOutboxWritter(),
                unitOfWork: new FakeUnitOfWork());

            Assert.IsType<BadRequestObjectResult>(response.Result);
        }
    }
}
