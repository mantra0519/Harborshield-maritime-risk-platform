using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Webhooks;
using HarborShield.Domain.Webhooks;
using Mediator;

namespace HarborShield.Application.Webhooks.Commands.RegisterWebhookEndpoint;

public class RegisterWebhookEndpointCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RegisterWebhookEndpointCommand, WebhookEndpointResponse>
{
    public async ValueTask<WebhookEndpointResponse> Handle(RegisterWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        var endpoint = WebhookEndpoint.Create(
            request.CustomerName,
            request.Url,
            request.Secret,
            request.SubscribedEventTypes);

        db.WebhookEndpoints.Add(endpoint);
        await db.SaveChangesAsync(cancellationToken);

        return endpoint.ToResponse();
    }
}
