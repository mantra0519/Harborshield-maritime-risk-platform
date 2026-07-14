using HarborShield.Contracts.Webhooks;
using Mediator;

namespace HarborShield.Application.Webhooks.Queries.GetWebhookEndpointById;

public record GetWebhookEndpointByIdQuery(Guid WebhookEndpointId) : IRequest<WebhookEndpointResponse>;
