namespace HarborShield.Contracts.RiskCases;

public record RiskCaseResponse(
    Guid Id,
    Guid VesselId,
    string CaseType,
    string Severity,
    int RiskScore,
    string Status,
    IReadOnlyList<string> Reasons,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    string? ResolutionNotes,
    string? CachedExplanation);
