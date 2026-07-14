namespace HarborShield.Application.RiskCases.Copilot;

public interface IRiskCaseExplainer
{
    Task<string> ExplainAsync(Guid riskCaseId, CancellationToken cancellationToken);
}
