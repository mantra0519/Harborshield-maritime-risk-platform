using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.Webhooks;
using HarborShield.Contracts.Events;
using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.RiskCases.Commands.CreateRiskCase;

public class CreateRiskCaseCommandHandler(IApplicationDbContext db, WebhookDeliveryEnqueuer webhookEnqueuer)
    : IRequestHandler<CreateRiskCaseCommand, RiskCaseResponse>
{
    public async ValueTask<RiskCaseResponse> Handle(CreateRiskCaseCommand request, CancellationToken cancellationToken)
    {
        var vesselExists = await db.Vessels.AnyAsync(v => v.Id == request.VesselId, cancellationToken);
        if (!vesselExists)
            throw new NotFoundException(nameof(Vessel), request.VesselId);

        var riskCase = RiskCase.Create(
            request.VesselId,
            request.CaseType,
            request.Severity,
            request.RiskScore,
            request.Reasons);

        db.RiskCases.Add(riskCase);

        var webhookPayload = new RiskCaseCreatedEvent(
            riskCase.Id,
            riskCase.VesselId,
            riskCase.CaseType.ToString(),
            riskCase.Severity.ToString(),
            riskCase.RiskScore,
            riskCase.Reasons,
            riskCase.CreatedAt);

        await webhookEnqueuer.EnqueueAsync(WebhookEventTypes.RiskCaseCreated, webhookPayload, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return riskCase.ToResponse();
    }
}
