using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureResilience
{
    public static IServiceCollection AddSeldonVigilResilience(this IServiceCollection services, IConfiguration configuration)
    {
        // Default resilience pipeline
        services.AddResiliencePipeline("default", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = configuration.GetValue("Resilience:Default:MaxRetries", 3),
                    Delay = TimeSpan.FromMilliseconds(configuration.GetValue("Resilience:Default:DelayMs", 200)),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = configuration.GetValue("Resilience:Default:FailureRatio", 0.5),
                    SamplingDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:Default:SamplingDurationSeconds", 10)),
                    MinimumThroughput = configuration.GetValue("Resilience:Default:MinimumThroughput", 10),
                    BreakDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:Default:BreakDurationSeconds", 30)),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddTimeout(TimeSpan.FromSeconds(configuration.GetValue("Resilience:Default:TimeoutSeconds", 10)));
        });

        // AI/LLM calls: longer timeouts, more aggressive circuit breaker
        services.AddResiliencePipeline("ai-llm", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = configuration.GetValue("Resilience:AI:MaxRetries", 2),
                    Delay = TimeSpan.FromSeconds(configuration.GetValue("Resilience:AI:DelaySeconds", 2)),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = configuration.GetValue("Resilience:AI:FailureRatio", 0.3),
                    SamplingDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:AI:SamplingDurationSeconds", 30)),
                    MinimumThroughput = configuration.GetValue("Resilience:AI:MinimumThroughput", 5),
                    BreakDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:AI:BreakDurationSeconds", 60)),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddTimeout(TimeSpan.FromSeconds(configuration.GetValue("Resilience:AI:TimeoutSeconds", 120)));
        });

        // Database operations: short retries, no circuit breaker
        services.AddResiliencePipeline("database", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = configuration.GetValue("Resilience:Database:MaxRetries", 2),
                    Delay = TimeSpan.FromMilliseconds(configuration.GetValue("Resilience:Database:DelayMs", 200)),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddTimeout(TimeSpan.FromSeconds(configuration.GetValue("Resilience:Database:TimeoutSeconds", 30)));
        });

        // Message broker: quick retries, short circuit
        services.AddResiliencePipeline("messaging", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = configuration.GetValue("Resilience:Messaging:MaxRetries", 3),
                    Delay = TimeSpan.FromMilliseconds(configuration.GetValue("Resilience:Messaging:DelayMs", 100)),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = configuration.GetValue("Resilience:Messaging:FailureRatio", 0.4),
                    SamplingDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:Messaging:SamplingDurationSeconds", 15)),
                    MinimumThroughput = configuration.GetValue("Resilience:Messaging:MinimumThroughput", 5),
                    BreakDuration = TimeSpan.FromSeconds(configuration.GetValue("Resilience:Messaging:BreakDurationSeconds", 15)),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                })
                .AddTimeout(TimeSpan.FromSeconds(configuration.GetValue("Resilience:Messaging:TimeoutSeconds", 10)));
        });

        // Enrichment for telemetry
        services.AddResilienceEnricher();

        return services;
    }
}
