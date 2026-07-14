using HarborShield.Contracts.RiskCases;
using HarborShield.Domain.RiskCases;
using Mediator;

namespace HarborShield.Application.RiskCases.Commands.CreateRiskCase;

public record CreateRiskCaseCommand(
    Guid VesselId,
    RiskCaseType CaseType,
    RiskSeverity Severity,
    int RiskScore,
    List<string> Reasons) : IRequest<RiskCaseResponse>;
