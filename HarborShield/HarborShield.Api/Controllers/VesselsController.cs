using HarborShield.Application.Vessels.Commands.RegisterVessel;
using HarborShield.Application.Vessels.Commands.SubmitVesselPosition;
using HarborShield.Application.Vessels.Queries.GetVesselById;
using HarborShield.Contracts.Vessels;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HarborShield.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/vessels")]
public class VesselsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<VesselResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<VesselResponse>> Register(
        [FromBody] RegisterVesselRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterVesselCommand(request.ImoNumber, request.Name, request.FlagCountry);
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { vesselId = result.Id }, result);
    }

    [HttpGet("{vesselId:guid}")]
    [ProducesResponseType<VesselResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VesselResponse>> GetById(Guid vesselId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVesselByIdQuery(vesselId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{vesselId:guid}/positions")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SubmitPosition(
        Guid vesselId,
        [FromBody] SubmitVesselPositionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitVesselPositionCommand(
            vesselId,
            request.Latitude,
            request.Longitude,
            request.SpeedKnots,
            request.HeadingDegrees,
            request.Destination,
            request.RecordedAt);

        var positionEventId = await mediator.Send(command, cancellationToken);

        return Accepted(new { positionEventId });
    }
}
