using Mediator;

namespace HarborShield.Application.RiskCases.Queries.ExplainRiskCase;

public record ExplainRiskCaseQuery(Guid RiskCaseId) : IRequest<string>;
