namespace HarborShield.Contracts.Vessels;

public record VesselResponse(
    Guid Id,
    string ImoNumber,
    string Name,
    string FlagCountry,
    DateTimeOffset CreatedAt);
