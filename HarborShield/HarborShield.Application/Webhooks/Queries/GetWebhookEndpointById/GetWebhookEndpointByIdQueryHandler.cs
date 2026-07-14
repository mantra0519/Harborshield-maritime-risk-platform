using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Webhooks;
using HarborShield.Domain.Webhooks;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Webhooks.Queries.GetWebhookEndpointById;

public class GetWebhookEndpointByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWebhookEndpointByIdQuery, WebhookEndpointResponse>
{
    public async ValueTask<WebhookEndpointResponse> Handle(GetWebhookEndpointByIdQuery request, CancellationToken cancellationToken)
    {
        var endpoint = await db.WebhookEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WebhookEndpointId, cancellationToken);

        if (endpoint is null)
            throw new NotFoundException(nameof(WebhookEndpoint), request.WebhookEndpointId);

        return endpoint.ToResponse();
    }
}
