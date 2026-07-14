namespace HarborShield.Domain.RiskCases;

public class RiskCase
{
    public Guid Id { get; private set; }
    public Guid VesselId { get; private set; }
    public RiskCaseType CaseType { get; private set; }
    public RiskSeverity Severity { get; private set; }
    public int RiskScore { get; private set; }
    public RiskCaseStatus Status { get; private set; }
    public List<string> Reasons { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public string? CachedExplanation { get; private set; }
    public DateTimeOffset? ExplanationCachedAt { get; private set; }

    private RiskCase()
    {
    }

    public static RiskCase Create(
        Guid vesselId,
        RiskCaseType caseType,
        RiskSeverity severity,
        int riskScore,
        IEnumerable<string> reasons)
    {
        if (riskScore is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(riskScore), "Risk score must be between 0 and 100.");

        return new RiskCase
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            CaseType = caseType,
            Severity = severity,
            RiskScore = riskScore,
            Status = RiskCaseStatus.Open,
            Reasons = reasons.ToList(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Acknowledge()
    {
        if (Status != RiskCaseStatus.Open)
            throw new InvalidOperationException($"Cannot acknowledge a case in {Status} status.");

        Status = RiskCaseStatus.Acknowledged;
    }

    public void Resolve(string notes)
    {
        if (Status == RiskCaseStatus.Resolved)
            throw new InvalidOperationException("Case is already resolved.");

        Status = RiskCaseStatus.Resolved;
        ResolutionNotes = notes;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void CacheExplanation(string explanation)
    {
        CachedExplanation = explanation;
        ExplanationCachedAt = DateTimeOffset.UtcNow;
    }
}
