using HarborShield.Contracts.Webhooks;
using HarborShield.Domain.Webhooks;

namespace HarborShield.Application.Webhooks;

public static class WebhookEndpointMappingExtensions
{
    // Deliberately excludes Secret - it's write-only once registered.
    public static WebhookEndpointResponse ToResponse(this WebhookEndpoint endpoint) => new(
        endpoint.Id,
        endpoint.CustomerName,
        endpoint.Url,
        endpoint.SubscribedEventTypes,
        endpoint.IsActive,
        endpoint.CreatedAt);
}
