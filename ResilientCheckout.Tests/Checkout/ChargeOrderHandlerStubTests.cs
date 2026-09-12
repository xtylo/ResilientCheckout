using ResilientCheckout.Application.Abstractions;
using ResilientCheckout.Application.Checkout;
using ResilientCheckout.Application.Commands;
using ResilientCheckout.Application.Idempotency;
using ResilientCheckout.Application.Outbox;
using ResilientCheckout.Domain.Outbox;
using ResilientCheckout.Domain.Payments;
using Xunit;

namespace ResilientCheckout.Tests.Checkout
{
    // STUB: StubPaymentProvider always returns the SAME canned response no matter
    // what's asked of it, and nobody verifies how or how many times it was called --
    // that's the key difference from a Mock (see ChargeOrderHandlerMockTests).
    // It's used to "get the payment provider out of the way" when what you actually
    // want to test is the rest of the handler (the final result it builds).
    //
    // Note: this file used to be called CheckoutControllerStubTests.cs -- the physical
    // name fell out of date after moving the orchestration into the handler (I couldn't
    // rename the file due to a temporary limitation of the bridge to your machine). You
    // can rename it in Visual Studio whenever you want; it doesn't affect compilation.
    public class ChargeOrderHandlerStubTests
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

        // Minimal Fakes -- just so the rest of the flow completes. They're not the focus
        // of this file (that's EFIdempotencyStoreFakeTests.cs).
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

        [Fact]
        public async Task HandleAsync_WhenPaymentProviderSucceeds_ReturnsSuccessWithPaymentResult()
        {
            var handler = new ChargeOrderHandler(
                new StubPaymentProvider(),
                new FakeIdempotencyStore(),
                new FakeOutboxWritter(),
                new FakeUnitOfWork());

            var command = new ChargeCardCommand { OrderId = 1 };

            var result = await handler.HandleAsync(command, "test-key-1");

            Assert.Equal(ChargeOrderOutcome.Success, result.Outcome);
            Assert.NotNull(result.PaymentResult);
            Assert.True(result.PaymentResult!.Succeed);
            Assert.Equal("stub-transaction-id", result.PaymentResult.TransactionId);
        }
    }
}
