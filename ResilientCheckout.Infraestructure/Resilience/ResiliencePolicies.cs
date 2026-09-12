using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using ResilientCheckout.Domain.Payments;

namespace ResilientCheckout.Infraestructure.Resilience
{
    // A single pipeline (retry + circuit breaker) to wrap any IPaymentProvider.
    // The order of .AddRetry(...) before .AddCircuitBreaker(...) matters: Retry sits
    // "on the outside" and Circuit Breaker "on the inside" — every retry attempt is seen by the
    // circuit breaker, so if the circuit is already open, the retry doesn't waste time
    // retrying against something we already know will fail fast.
    public static class ResiliencePolicies
    {
        public static ResiliencePipeline<PaymentResult> CreatePaymentProviderPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder<PaymentResult>()
                .AddRetry(new RetryStrategyOptions<PaymentResult>
                {
                    // Only retries technical failures of the provider (PaymentProviderUnavailableException).
                    // A PaymentResult with Succeed = false (e.g. card declined) does NOT fall here,
                    // so Polly never retries a business rejection.
                    ShouldHandle = new PredicateBuilder<PaymentResult>()
                        .Handle<PaymentProviderUnavailableException>(),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Retry #{AttemptNumber} after payment provider failure: {Message}",
                            args.AttemptNumber + 1,
                            args.Outcome.Exception?.Message);
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<PaymentResult>
                {
                    ShouldHandle = new PredicateBuilder<PaymentResult>()
                        .Handle<PaymentProviderUnavailableException>(),
                    // Of the most recent calls within the sampling window, if at least
                    // 50% failed (and there were at least 4 calls to have a real sample),
                    // the circuit opens.
                    FailureRatio = 0.5,
                    MinimumThroughput = 4,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    // While open, fail-fast: the provider isn't even called.
                    // After this time, it moves to Half-Open and lets ONE trial call through.
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "Circuit breaker OPENED — the payment provider will not be called for {BreakDuration}.",
                            args.BreakDuration);
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("Circuit breaker HALF-OPEN — trying the next call.");
                        return default;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("Circuit breaker CLOSED — the payment provider recovered.");
                        return default;
                    }
                })
                .Build();
        }
    }
}
