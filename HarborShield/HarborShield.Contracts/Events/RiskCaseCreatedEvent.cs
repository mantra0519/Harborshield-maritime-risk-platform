namespace HarborShield.Contracts.Events;

public record RiskCaseCreatedEvent(
    Guid RiskCaseId,
    Guid VesselId,
    string CaseType,
    string Severity,
    int RiskScore,
    IReadOnlyList<string> Reasons,
    DateTimeOffset CreatedAt);
