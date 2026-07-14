using HarborShield.Application.RiskCases.Commands.CreateRiskCase;
using HarborShield.Application.RiskCases.Commands.ResolveRiskCase;
using HarborShield.Application.RiskCases.Queries.ExplainRiskCase;
using HarborShield.Application.RiskCases.Queries.GetRiskCaseById;
using HarborShield.Application.RiskCases.Queries.ListRiskCases;
using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HarborShield.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/risk-cases")]
public class RiskCasesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RiskCaseResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RiskCaseResponse>>> List(
        [FromQuery] RiskCaseStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListRiskCasesQuery(status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{riskCaseId:guid}")]
    [ProducesResponseType<RiskCaseResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskCaseResponse>> GetById(Guid riskCaseId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRiskCaseByIdQuery(riskCaseId), cancellationToken);
        return Ok(result);
    }

    /// <remarks>
    /// Normally risk cases are created by the anomaly-detection worker. This endpoint exists
    /// so the flow can be demoed/tested before the worker is wired up.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<RiskCaseResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RiskCaseResponse>> Create(
        [FromBody] CreateRiskCaseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { riskCaseId = result.Id }, result);
    }

    [HttpPost("{riskCaseId:guid}/resolve")]
    [ProducesResponseType<RiskCaseResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskCaseResponse>> Resolve(
        Guid riskCaseId,
        [FromBody] ResolveRiskCaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ResolveRiskCaseCommand(riskCaseId, request.Notes), cancellationToken);
        return Ok(result);
    }

    /// <remarks>
    /// Runs entirely on local models (LLamaSharp + a GGUF model on disk) - no external API
    /// calls, no API keys. First call for a given process can be slow (loads model weights
    /// into memory); subsequent calls reuse the already-loaded weights.
    /// </remarks>
    [HttpGet("{riskCaseId:guid}/explain")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> Explain(Guid riskCaseId, CancellationToken cancellationToken)
    {
        var explanation = await mediator.Send(new ExplainRiskCaseQuery(riskCaseId), cancellationToken);
        return Ok(explanation);
    }
}
