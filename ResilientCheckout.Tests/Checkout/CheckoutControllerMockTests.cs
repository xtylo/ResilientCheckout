using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
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
    // MOCK: a diferencia del Stub (que solo regresa datos enlatados y a nadie le
    // importa cómo se le llamó), aquí lo que se prueba es la INTERACCIÓN misma --
    // que se haya llamado (o NO llamado) un método específico, con ciertos argumentos,
    // cierta cantidad de veces. Eso es invisible mirando solo el valor de retorno del
    // controller; hace falta un objeto que registre las llamadas y las pueda verificar
    // después del hecho (Moq: .Verify(...)).
    public class CheckoutControllerMockTests
    {
        private static CheckoutController CreateController(string idempotencyKey)
        {
            var controller = new CheckoutController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.ControllerContext.HttpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;
            return controller;
        }

        [Fact]
        public async Task Charge_WhenIdempotencyReservationFails_NeverCallsPaymentProvider()
        {
            var idempotencyStoreMock = new Mock<IIdempotencyStore>();
            idempotencyStoreMock
                .Setup(s => s.TryReserveAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // ya existe una reserva -- carrera perdida / reintento del cliente

            var paymentProviderMock = new Mock<IPaymentProvider>();

            var controller = CreateController("duplicated-key");
            var command = new ChargeCardCommand { OrderId = 1 };

            var response = await controller.Charge(
                orderId: 1,
                command: command,
                paymentProvider: paymentProviderMock.Object,
                idempotencyStore: idempotencyStoreMock.Object,
                outboxWriter: Mock.Of<IOutboxWritter>(),
                unitOfWork: Mock.Of<IUnitOfWork>());

            Assert.IsType<ConflictObjectResult>(response.Result);

            // Lo interesante no es el resultado -- es que el proveedor de pago JAMÁS
            // debió tocarse. Un Stub no puede probar una ausencia de llamada; hace
            // falta un Mock que registre y verifique la interacción.
            paymentProviderMock.Verify(
                p => p.ChargeAsync(It.IsAny<ChargeInstruction>()),
                Times.Never);
        }

        [Fact]
        public async Task Charge_WhenPaymentSucceeds_WritesOutboxMessageAndSavesExactlyOnce()
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

            var controller = CreateController("new-key");
            var command = new ChargeCardCommand { OrderId = 1 };

            await controller.Charge(
                orderId: 1,
                command: command,
                paymentProvider: paymentProviderMock.Object,
                idempotencyStore: idempotencyStoreMock.Object,
                outboxWriter: outboxWriterMock.Object,
                unitOfWork: unitOfWorkMock.Object);

            outboxWriterMock.Verify(
                o => o.Add(EventType.PaymentSucceeded, It.IsAny<object>()),
                Times.Once);

            unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
