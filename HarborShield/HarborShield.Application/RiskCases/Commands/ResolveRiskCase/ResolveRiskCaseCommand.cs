using HarborShield.Contracts.RiskCases;
using Mediator;

namespace HarborShield.Application.RiskCases.Commands.ResolveRiskCase;

public record ResolveRiskCaseCommand(Guid RiskCaseId, string Notes) : IRequest<RiskCaseResponse>;
