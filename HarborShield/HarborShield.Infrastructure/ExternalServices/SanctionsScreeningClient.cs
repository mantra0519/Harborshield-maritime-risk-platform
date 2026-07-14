using System.Net.Http.Json;
using HarborShield.Application.Vessels.Sanctions;

namespace HarborShield.Infrastructure.ExternalServices;

public class SanctionsScreeningClient(HttpClient httpClient) : ISanctionsScreeningClient
{
    public async Task<SanctionsScreeningResult> ScreenAsync(SanctionsScreeningRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("api/fake-vendor/sanctions-screening", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SanctionsScreeningResult>(cancellationToken);
        return result ?? new SanctionsScreeningResult(false, null, 0);
    }
}
