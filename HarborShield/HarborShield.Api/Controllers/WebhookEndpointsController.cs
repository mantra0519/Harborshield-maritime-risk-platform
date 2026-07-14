using HarborShield.Application.Webhooks.Commands.RegisterWebhookEndpoint;
using HarborShield.Application.Webhooks.Queries.GetWebhookEndpointById;
using HarborShield.Contracts.Webhooks;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HarborShield.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/webhook-endpoints")]
public class WebhookEndpointsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WebhookEndpointResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WebhookEndpointResponse>> Register(
        [FromBody] RegisterWebhookEndpointRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterWebhookEndpointCommand(
            request.CustomerName,
            request.Url,
            request.Secret,
            request.SubscribedEventTypes);

        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { webhookEndpointId = result.Id }, result);
    }

    [HttpGet("{webhookEndpointId:guid}")]
    [ProducesResponseType<WebhookEndpointResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WebhookEndpointResponse>> GetById(Guid webhookEndpointId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWebhookEndpointByIdQuery(webhookEndpointId), cancellationToken);
        return Ok(result);
    }
}
