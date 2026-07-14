using FluentAssertions;
using HarborShield.Application.Vessels.Sanctions;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace HarborShield.IntegrationTests.Sanctions;

/// <summary>
/// Exercises the actual resilience pipeline (retry/timeout) configured in Infrastructure's
/// DependencyInjection, against a real HTTP mock rather than asserting on Polly config values.
/// Both scenarios run in one test method deliberately: the circuit breaker's rolling failure
/// window is shared across the whole class fixture, and splitting this into two test methods
/// makes the (harmless, single) injected failure in one test trip the breaker for the other.
/// </summary>
public class SanctionsScreeningClientTests(HarborShieldApiFactory factory) : IClassFixture<HarborShieldApiFactory>
{
    [Fact]
    public async Task ScreenAsync_RetriesTransientFailureThenReturnsCleanMatchResult()
    {
        const string scenario = "retry-then-success";
        factory.SanctionsVendorMock.Reset();

        factory.SanctionsVendorMock
            .Given(Request.Create().WithPath("/api/fake-vendor/sanctions-screening").UsingPost())
            .InScenario(scenario)
            .WillSetStateTo("second-call")
            .RespondWith(Response.Create().WithStatusCode(503));

        factory.SanctionsVendorMock
            .Given(Request.Create().WithPath("/api/fake-vendor/sanctions-screening").UsingPost())
            .InScenario(scenario)
            .WhenStateIs("second-call")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"isMatch":false,"matchedEntity":null,"confidence":0}"""));

        using (var scope = factory.Services.CreateScope())
        {
            var client = scope.ServiceProvider.GetRequiredService<ISanctionsScreeningClient>();
            var result = await client.ScreenAsync(new SanctionsScreeningRequest("MV Resilience Test", "Panama"), CancellationToken.None);

            result.IsMatch.Should().BeFalse();
            factory.SanctionsVendorMock.LogEntries.Should().HaveCount(2, "the client should have retried once after the 503");
        }

        factory.SanctionsVendorMock.Reset();
        factory.SanctionsVendorMock
            .Given(Request.Create().WithPath("/api/fake-vendor/sanctions-screening").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"isMatch":true,"matchedEntity":"MV Shadow Runner","confidence":0.97}"""));

        using (var scope = factory.Services.CreateScope())
        {
            var client = scope.ServiceProvider.GetRequiredService<ISanctionsScreeningClient>();
            var result = await client.ScreenAsync(new SanctionsScreeningRequest("MV Shadow Runner", "Unknown"), CancellationToken.None);

            result.IsMatch.Should().BeTrue();
            result.MatchedEntity.Should().Be("MV Shadow Runner");
            result.Confidence.Should().Be(0.97);
        }
    }
}
