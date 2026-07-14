namespace HarborShield.Application.Vessels.Sanctions;

public record SanctionsScreeningResult(bool IsMatch, string? MatchedEntity, double Confidence);
