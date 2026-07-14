using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HarborShield.Contracts.RiskCases;
using HarborShield.Contracts.Vessels;

namespace HarborShield.IntegrationTests.RiskCases;

public class RiskCasesApiTests(HarborShieldApiFactory factory) : IClassFixture<HarborShieldApiFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    private async Task<Guid> RegisterVesselAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/vessels",
            new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV Risk Case Test", "Panama"));

        var vessel = await response.Content.ReadFromJsonAsync<VesselResponse>();
        return vessel!.Id;
    }

    [Fact]
    public async Task FullLifecycle_CreateGetListResolve_TransitionsCorrectly()
    {
        var vesselId = await RegisterVesselAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/risk-cases", new
        {
            vesselId,
            caseType = "CargoAnomaly",
            severity = "Medium",
            riskScore = 55,
            reasons = new[] { "Integration test reason" }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<RiskCaseResponse>();
        created!.Status.Should().Be("Open");

        var getResponse = await _client.GetAsync($"/api/risk-cases/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await _client.GetAsync("/api/risk-cases?status=Open");
        var openCases = await listResponse.Content.ReadFromJsonAsync<List<RiskCaseResponse>>();
        openCases.Should().Contain(c => c.Id == created.Id);

        var resolveResponse = await _client.PostAsJsonAsync(
            $"/api/risk-cases/{created.Id}/resolve",
            new ResolveRiskCaseRequest("Confirmed benign in integration test"));

        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<RiskCaseResponse>();
        resolved!.Status.Should().Be("Resolved");
        resolved.ResolutionNotes.Should().Be("Confirmed benign in integration test");
    }

    [Fact]
    public async Task Create_UnknownVessel_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/risk-cases", new
        {
            vesselId = Guid.NewGuid(),
            caseType = "RouteDeviation",
            severity = "High",
            riskScore = 80,
            reasons = new[] { "reason" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Resolve_AlreadyResolvedCase_Returns500ProblemDetails()
    {
        var vesselId = await RegisterVesselAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/risk-cases", new
        {
            vesselId,
            caseType = "TrackingGap",
            severity = "Low",
            riskScore = 20,
            reasons = new[] { "reason" }
        });
        var created = await createResponse.Content.ReadFromJsonAsync<RiskCaseResponse>();

        await _client.PostAsJsonAsync($"/api/risk-cases/{created!.Id}/resolve", new ResolveRiskCaseRequest("First"));
        var secondResolve = await _client.PostAsJsonAsync($"/api/risk-cases/{created.Id}/resolve", new ResolveRiskCaseRequest("Second"));

        secondResolve.IsSuccessStatusCode.Should().BeFalse();
    }
}
