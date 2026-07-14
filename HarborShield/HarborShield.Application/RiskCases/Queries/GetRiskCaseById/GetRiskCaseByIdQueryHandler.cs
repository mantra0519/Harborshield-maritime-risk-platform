using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.RiskCases.Queries.GetRiskCaseById;

public class GetRiskCaseByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRiskCaseByIdQuery, RiskCaseResponse>
{
    public async ValueTask<RiskCaseResponse> Handle(GetRiskCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var riskCase = await db.RiskCases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RiskCaseId, cancellationToken);

        if (riskCase is null)
            throw new NotFoundException(nameof(RiskCase), request.RiskCaseId);

        return riskCase.ToResponse();
    }
}
