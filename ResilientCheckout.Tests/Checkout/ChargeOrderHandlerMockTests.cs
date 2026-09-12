using Moq;
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
    // MOCK: unlike a Stub (which just returns canned data and nobody cares
    // how it was called), here the INTERACTION itself is verified -- that a specific
    // method was (or was NOT) called, with certain arguments, a certain number of times.
    // Now that the orchestration has moved to ChargeOrderHandler, these
    // verifications are done directly against the handler, which is where it actually lives.
    //
    // Note: this file used to be called CheckoutControllerMockTests.cs -- the physical name
    // fell out of date after the refactor (I couldn't rename it due to a temporary
    // limitation of the bridge to your machine). You can rename it in Visual Studio whenever
    // you want; it doesn't affect compilation.
    public class ChargeOrderHandlerMockTests
    {
        [Fact]
        public async Task HandleAsync_WhenIdempotencyReservationFails_NeverCallsPaymentProvider()
        {
            var idempotencyStoreMock = new Mock<IIdempotencyStore>();
            idempotencyStoreMock
                .Setup(s => s.TryReserveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // a reservation already exists -- lost race / client retry

            var paymentProviderMock = new Mock<IPaymentProvider>();

            var handler = new ChargeOrderHandler(
                paymentProviderMock.Object,
                idempotencyStoreMock.Object,
                Mock.Of<IOutboxWritter>(),
                Mock.Of<IUnitOfWork>());

            var command = new ChargeCardCommand { OrderId = 1 };

            var result = await handler.HandleAsync(command, "duplicated-key");

            Assert.Equal(ChargeOrderOutcome.AlreadyProcessing, result.Outcome);

            // The interesting part isn't the result -- it's that the payment provider
            // must NEVER be touched. A Stub can't prove the absence of a call; you need
            // a Mock that records and verifies the interaction.
            paymentProviderMock.Verify(
                p => p.ChargeAsync(It.IsAny<ChargeInstruction>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WhenPaymentSucceeds_WritesOutboxMessageAndSavesExactlyOnce()
        {
            var paymentProviderMock = new Mock<IPaymentProvider>();
            paymentProviderMock
                .Setup(p => p.ChargeAsync(It.IsAny<ChargeInstruction>()))
                .ReturnsAsync(new PaymentResult
                {
                    OrderId = 1,
                    Succeed = true,
                    Provider = PaymentProvider.Stripe,
                    TransactionId = "tx-mock-1"
                });

            var idempotencyStoreMock = new Mock<IIdempotencyStore>();
            idempotencyStoreMock
                .Setup(s => s.TryReserveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var outboxWriterMock = new Mock<IOutboxWritter>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var handler = new ChargeOrderHandler(
                paymentProviderMock.Object,
                idempotencyStoreMock.Object,
                outboxWriterMock.Object,
                unitOfWorkMock.Object);

            var command = new ChargeCardCommand { OrderId = 1 };

            var result = await handler.HandleAsync(command, "new-key");

            Assert.Equal(ChargeOrderOutcome.Success, result.Outcome);

            outboxWriterMock.Verify(
                o => o.Add(EventType.PaymentSucceeded, It.IsAny<object>()),
                Times.Once);

            unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
