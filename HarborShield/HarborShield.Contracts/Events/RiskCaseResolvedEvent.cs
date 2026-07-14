namespace HarborShield.Contracts.Events;

public record RiskCaseResolvedEvent(Guid RiskCaseId, Guid VesselId, DateTimeOffset ResolvedAt);
