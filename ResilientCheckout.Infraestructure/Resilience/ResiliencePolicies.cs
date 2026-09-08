using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Infraestructure.Resilience
{
    // Un solo pipeline (retry + circuit breaker) para envolver cualquier IPaymentProvider.
    // El orden de .AddRetry(...) antes de .AddCircuitBreaker(...) importa: Retry queda
    // "por fuera" y Circuit Breaker "por dentro" — cada intento del retry es visto por el
    // circuit breaker, así que si el circuito ya está abierto, el retry no pierde tiempo
    // reintentando contra algo que ya sabemos que va a fallar rápido.
    public static class ResiliencePolicies
    {
        public static ResiliencePipeline<PaymentResult> CreatePaymentProviderPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder<PaymentResult>()
                .AddRetry(new RetryStrategyOptions<PaymentResult>
                {
                    // Solo reintenta fallas técnicas del provider (PaymentProviderUnavailableException).
                    // Un PaymentResult con Succeed = false (ej. tarjeta rechazada) NO cae aquí,
                    // así que Polly nunca reintenta un rechazo de negocio.
                    ShouldHandle = new PredicateBuilder<PaymentResult>()
                        .Handle<PaymentProviderUnavailableException>(),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Reintento #{AttemptNumber} tras falla del payment provider: {Message}",
                            args.AttemptNumber + 1,
                            args.Outcome.Exception?.Message);
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<PaymentResult>
                {
                    ShouldHandle = new PredicateBuilder<PaymentResult>()
                        .Handle<PaymentProviderUnavailableException>(),
                    // De las últimas llamadas dentro de la ventana de muestreo, si al menos
                    // el 50% falló (y hubo al menos 4 llamadas para tener una muestra real),
                    // se abre el circuito.
                    FailureRatio = 0.5,
                    MinimumThroughput = 4,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    // Mientras está abierto, fail-fast: ni siquiera se llama al provider.
                    // Tras este tiempo, pasa a Half-Open y deja pasar UNA llamada de prueba.
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "Circuit breaker ABIERTO — se deja de llamar al payment provider por {BreakDuration}.",
                            args.BreakDuration);
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("Circuit breaker en HALF-OPEN — probando con la siguiente llamada.");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("Circuit breaker CERRADO — el payment provider se recuperó.");
                        return default;
                    }
                })
                .Build();
        }
    }
}
