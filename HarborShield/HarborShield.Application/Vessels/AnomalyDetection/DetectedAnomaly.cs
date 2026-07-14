using HarborShield.Domain.RiskCases;

namespace HarborShield.Application.Vessels.AnomalyDetection;

public record DetectedAnomaly(RiskCaseType CaseType, RiskSeverity Severity, int RiskScore, IReadOnlyList<string> Reasons);
