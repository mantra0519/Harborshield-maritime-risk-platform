using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.Webhooks;
using HarborShield.Contracts.Events;
using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.RiskCases.Commands.ResolveRiskCase;

public class ResolveRiskCaseCommandHandler(IApplicationDbContext db, WebhookDeliveryEnqueuer webhookEnqueuer)
    : IRequestHandler<ResolveRiskCaseCommand, RiskCaseResponse>
{
    public async ValueTask<RiskCaseResponse> Handle(ResolveRiskCaseCommand request, CancellationToken cancellationToken)
    {
        var riskCase = await db.RiskCases
            .FirstOrDefaultAsync(r => r.Id == request.RiskCaseId, cancellationToken);

        if (riskCase is null)
            throw new NotFoundException(nameof(RiskCase), request.RiskCaseId);

        riskCase.Resolve(request.Notes);

        var webhookPayload = new RiskCaseResolvedEvent(riskCase.Id, riskCase.VesselId, riskCase.ResolvedAt!.Value);
        await webhookEnqueuer.EnqueueAsync(WebhookEventTypes.RiskCaseResolved, webhookPayload, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return riskCase.ToResponse();
    }
}
