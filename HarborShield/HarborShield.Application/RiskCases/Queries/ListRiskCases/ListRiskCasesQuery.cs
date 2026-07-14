using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using Mediator;

namespace HarborShield.Application.RiskCases.Queries.ListRiskCases;

public record ListRiskCasesQuery(RiskCaseStatus? Status) : IRequest<IReadOnlyList<RiskCaseResponse>>;
