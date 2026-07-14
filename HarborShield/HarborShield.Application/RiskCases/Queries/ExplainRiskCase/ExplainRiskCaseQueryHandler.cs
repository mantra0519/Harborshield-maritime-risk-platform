using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.RiskCases.Copilot;
using HarborShield.Domain.RiskCases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.RiskCases.Queries.ExplainRiskCase;

public class ExplainRiskCaseQueryHandler(IRiskCaseExplainer explainer, IApplicationDbContext db)
    : IRequestHandler<ExplainRiskCaseQuery, string>
{
    public async ValueTask<string> Handle(ExplainRiskCaseQuery request, CancellationToken cancellationToken)
    {
        var riskCase = await db.RiskCases.SingleOrDefaultAsync(r => r.Id == request.RiskCaseId, cancellationToken);
        if (riskCase is null)
            throw new NotFoundException(nameof(RiskCase), request.RiskCaseId);

        if (riskCase.CachedExplanation is not null)
            return riskCase.CachedExplanation;

        var explanation = await explainer.ExplainAsync(request.RiskCaseId, cancellationToken);

        riskCase.CacheExplanation(explanation);
        await db.SaveChangesAsync(cancellationToken);

        return explanation;
    }
}
