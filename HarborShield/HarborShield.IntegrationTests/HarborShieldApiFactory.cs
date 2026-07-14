using HarborShield.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace HarborShield.IntegrationTests;

/// <summary>
/// Spins up a real Postgres (our custom PostGIS+pgvector image, so migrations succeed exactly
/// as they do locally) and a WireMock server standing in for the sanctions vendor. RiskCopilot
/// model paths are left unset - no test in this project calls the /explain endpoint, so the
/// (Lazy) model weights are never actually loaded.
/// </summary>
public class HarborShieldApiFactory : WebApplicationFactory<Api.Program>, IAsyncLifetime
{
    public const string TestApiKey = "test-api-key-for-integration-tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("harborshield-maritime-risk-platform-postgres:latest")
        .WithDatabase("harborshield_test")
        .WithUsername("harborshield")
        .WithPassword("harborshield")
        .Build();

    public WireMockServer SanctionsVendorMock { get; private set; } = null!;

    /// <summary>An HttpClient pre-configured with the test API key, for calling the protected endpoints.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        return client;
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        SanctionsVendorMock = WireMockServer.Start();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HarborShieldDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HarborShieldDb"] = _postgres.GetConnectionString(),
                ["SanctionsVendor:BaseUrl"] = SanctionsVendorMock?.Url ?? "http://localhost:1/",
                ["RiskCopilot:GenerationModelPath"] = "not-used-in-tests.gguf",
                ["RiskCopilot:EmbeddingModelPath"] = "not-used-in-tests.gguf",
                ["Auth:ApiKey"] = TestApiKey
            });
        });
    }

    public new async Task DisposeAsync()
    {
        SanctionsVendorMock.Stop();
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
