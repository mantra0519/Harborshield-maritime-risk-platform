using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;

namespace HarborShield.Application.RiskCases;

public static class RiskCaseMappingExtensions
{
    public static RiskCaseResponse ToResponse(this RiskCase riskCase) => new(
        riskCase.Id,
        riskCase.VesselId,
        riskCase.CaseType.ToString(),
        riskCase.Severity.ToString(),
        riskCase.RiskScore,
        riskCase.Status.ToString(),
        riskCase.Reasons,
        riskCase.CreatedAt,
        riskCase.ResolvedAt,
        riskCase.ResolutionNotes,
        riskCase.CachedExplanation);
}
