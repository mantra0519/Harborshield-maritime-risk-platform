using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HarborShield.Contracts.Vessels;

namespace HarborShield.IntegrationTests.Vessels;

public class VesselsApiTests(HarborShieldApiFactory factory) : IClassFixture<HarborShieldApiFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Register_WithoutApiKey_Returns401()
    {
        using var unauthenticatedClient = factory.CreateClient();

        var response = await unauthenticatedClient.PostAsJsonAsync(
            "/api/vessels",
            new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV No Auth Test", "Panama"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201WithVessel()
    {
        var request = new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV Integration Test", "Panama");

        var response = await _client.PostAsJsonAsync("/api/vessels", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var vessel = await response.Content.ReadFromJsonAsync<VesselResponse>();
        vessel!.ImoNumber.Should().Be(request.ImoNumber);
        vessel.Name.Should().Be("MV Integration Test");
    }

    [Fact]
    public async Task Register_DuplicateImoNumber_Returns409()
    {
        var request = new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV Duplicate Test", "Liberia");
        await _client.PostAsJsonAsync("/api/vessels", request);

        var response = await _client.PostAsJsonAsync("/api/vessels", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/vessels", new RegisterVesselRequest("", "", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_UnknownVessel_Returns404()
    {
        var response = await _client.GetAsync($"/api/vessels/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitPosition_KnownVessel_Returns202()
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/vessels",
            new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV Position Test", "Bahamas"));
        var vessel = await registerResponse.Content.ReadFromJsonAsync<VesselResponse>();

        var positionResponse = await _client.PostAsJsonAsync(
            $"/api/vessels/{vessel!.Id}/positions",
            new SubmitVesselPositionRequest(9.05, -79.68, 14.2, 85, "Port of Balboa", DateTimeOffset.UtcNow));

        positionResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SubmitPosition_RepeatedWithSameIdempotencyKey_DoesNotCreateDuplicate()
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/vessels",
            new RegisterVesselRequest($"IMO-{Guid.NewGuid():N}"[..12], "MV Idempotency Test", "Bahamas"));
        var vessel = await registerResponse.Content.ReadFromJsonAsync<VesselResponse>();

        var body = new SubmitVesselPositionRequest(9.05, -79.68, 14.2, 85, "Port of Balboa", DateTimeOffset.UtcNow);
        var idempotencyKey = Guid.NewGuid().ToString();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/vessels/{vessel!.Id}/positions")
        {
            Content = JsonContent.Create(body)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var firstResponse = await _client.SendAsync(firstRequest);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/vessels/{vessel.Id}/positions")
        {
            Content = JsonContent.Create(body)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var secondResponse = await _client.SendAsync(secondRequest);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();

        secondResponse.StatusCode.Should().Be(firstResponse.StatusCode);
        secondResult!["positionEventId"].Should().Be(firstResult!["positionEventId"]);
    }
}
