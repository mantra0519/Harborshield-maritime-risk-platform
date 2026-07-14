using HarborShield.Contracts.RiskCases;
using Mediator;

namespace HarborShield.Application.RiskCases.Queries.GetRiskCaseById;

public record GetRiskCaseByIdQuery(Guid RiskCaseId) : IRequest<RiskCaseResponse>;
