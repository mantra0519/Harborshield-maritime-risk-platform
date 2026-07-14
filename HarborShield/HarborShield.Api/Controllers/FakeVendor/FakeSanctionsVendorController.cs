using HarborShield.Application.Vessels.Sanctions;
using Microsoft.AspNetCore.Mvc;

namespace HarborShield.Api.Controllers.FakeVendor;

/// <summary>
/// Stands in for a real external sanctions/ownership screening vendor so
/// SanctionsScreeningClient's retry/circuit-breaker/timeout pipeline has something realistic
/// to talk to locally (including simulated outages). Point SanctionsVendor:BaseUrl at a real
/// vendor in production instead of this controller.
/// </summary>
[ApiController]
[Route("api/fake-vendor/sanctions-screening")]
public class FakeSanctionsVendorController : ControllerBase
{
    private static readonly string[] WatchlistNames = ["MV Shadow Runner", "MV Sanction Buster"];

    [HttpPost]
    public async Task<ActionResult<SanctionsScreeningResult>> Screen(
        [FromBody] SanctionsScreeningRequest request,
        CancellationToken cancellationToken)
    {
        var roll = Random.Shared.NextDouble();

        if (roll < 0.1)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (roll < 0.2)
            await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);

        var isMatch = WatchlistNames.Any(name => string.Equals(name, request.VesselName, StringComparison.OrdinalIgnoreCase));

        var result = isMatch
            ? new SanctionsScreeningResult(true, request.VesselName, 0.97)
            : new SanctionsScreeningResult(false, null, 0);

        return Ok(result);
    }
}
