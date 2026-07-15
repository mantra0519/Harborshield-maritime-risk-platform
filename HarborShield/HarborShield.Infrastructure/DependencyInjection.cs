using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.Vessels.Sanctions;
using HarborShield.Infrastructure.ExternalServices;
using HarborShield.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace HarborShield.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Both of these resolve the connection string lazily, from a fully-built IServiceProvider/
        // IConfiguration, rather than capturing it once eagerly here - eager capture happens before
        // WebApplicationFactory's test overrides (e.g. the Testcontainers connection string) are
        // applied, so tests would silently fall back to whatever appsettings.* has instead of the
        // isolated test database. Same reasoning as the SanctionsScreeningClient client below.
        static string GetConnectionString(IServiceProvider sp) =>
            sp.GetRequiredService<IConfiguration>().GetConnectionString("HarborShieldDb")
                ?? throw new InvalidOperationException("Connection string 'HarborShieldDb' is not configured.");

        services.AddDbContext<HarborShieldDbContext>((sp, options) =>
            options.UseNpgsql(GetConnectionString(sp), npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.UseVector();
            }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<HarborShieldDbContext>());

        // Tagged "ready" (not "live") so /alive stays a lightweight "is the process running"
        // check, while /health (readiness) genuinely verifies the database is reachable.
        services.AddHealthChecks()
            .AddNpgSql(GetConnectionString, name: "postgres", tags: ["ready"]);

        services.AddHttpClient<ISanctionsScreeningClient, SanctionsScreeningClient>((sp, client) =>
            {
                // Resolved per-client-creation (not captured at registration time) so
                // config-override-based tests (WebApplicationFactory + WireMock) work correctly -
                // this method runs before the host finishes building, but IHttpClientFactory
                // doesn't actually create the client until something first requests it.
                var config = sp.GetRequiredService<IConfiguration>();
                var sanctionsVendorBaseUrl = config["SanctionsVendor:BaseUrl"]
                    ?? throw new InvalidOperationException("Configuration 'SanctionsVendor:BaseUrl' is not set.");

                client.BaseAddress = new Uri(sanctionsVendorBaseUrl);
                // The resilience pipeline below owns the timeout budget, not HttpClient itself.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("sanctions-vendor", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(200)
                });

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    // Higher than the textbook minimum: retry sits outside the circuit breaker in
                    // this pipeline, so each individual retry attempt (not just the logical call)
                    // counts as its own sample. A low threshold here made a handful of test-induced
                    // transient failures enough to trip the breaker; 10 gives real headroom before
                    // tripping while still reacting well within the 30s sampling window.
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(15)
                });

                builder.AddTimeout(TimeSpan.FromSeconds(5));
            });

        // No circuit breaker here: this client is shared across every customer's endpoint, and
        // one customer's outage shouldn't trip a breaker for everyone else. WebhookDelivery's
        // own MaxAttempts cap already stops retrying a specific delivery after enough failures.
        services.AddHttpClient(WebhookDeliveryHttpClientName.Name)
            .AddResilienceHandler("webhook-delivery", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(200)
                });

                builder.AddTimeout(TimeSpan.FromSeconds(5));
            });

        return services;
    }
}
