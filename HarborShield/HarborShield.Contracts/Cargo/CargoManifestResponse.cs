namespace HarborShield.Contracts.Cargo;

public record CargoManifestResponse(
    Guid Id,
    Guid VesselId,
    string OriginPort,
    string DestinationPort,
    string ShipperName,
    string ReceiverName,
    double DeclaredWeightKg,
    bool IsHazardous,
    DateTimeOffset SubmittedAt);
