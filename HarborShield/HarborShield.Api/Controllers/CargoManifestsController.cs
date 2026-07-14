using HarborShield.Application.Cargo.Commands.SubmitCargoManifest;
using HarborShield.Contracts.Cargo;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HarborShield.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/vessels/{vesselId:guid}/cargo-manifests")]
public class CargoManifestsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CargoManifestResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CargoManifestResponse>> Submit(
        Guid vesselId,
        [FromBody] SubmitCargoManifestRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitCargoManifestCommand(
            vesselId,
            request.OriginPort,
            request.DestinationPort,
            request.ShipperName,
            request.ReceiverName,
            request.DeclaredWeightKg,
            request.IsHazardous);

        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Submit), new { vesselId, manifestId = result.Id }, result);
    }
}
