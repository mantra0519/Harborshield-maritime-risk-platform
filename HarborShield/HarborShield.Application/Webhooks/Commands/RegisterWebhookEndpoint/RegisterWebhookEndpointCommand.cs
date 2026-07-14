using HarborShield.Contracts.Webhooks;
using Mediator;

namespace HarborShield.Application.Webhooks.Commands.RegisterWebhookEndpoint;

public record RegisterWebhookEndpointCommand(
    string CustomerName,
    string Url,
    string Secret,
    List<string> SubscribedEventTypes) : IRequest<WebhookEndpointResponse>;
