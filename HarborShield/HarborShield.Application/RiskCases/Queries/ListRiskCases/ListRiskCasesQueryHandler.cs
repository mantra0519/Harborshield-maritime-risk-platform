using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.RiskCases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.RiskCases.Queries.ListRiskCases;

public class ListRiskCasesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListRiskCasesQuery, IReadOnlyList<RiskCaseResponse>>
{
    public async ValueTask<IReadOnlyList<RiskCaseResponse>> Handle(ListRiskCasesQuery request, CancellationToken cancellationToken)
    {
        var query = db.RiskCases.AsNoTracking().AsQueryable();

        if (request.Status is not null)
            query = query.Where(r => r.Status == request.Status);

        var riskCases = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return riskCases.Select(r => r.ToResponse()).ToList();
    }
}
