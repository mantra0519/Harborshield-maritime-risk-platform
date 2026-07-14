namespace HarborShield.Contracts.Cargo;

public record SubmitCargoManifestRequest(
    string OriginPort,
    string DestinationPort,
    string ShipperName,
    string ReceiverName,
    double DeclaredWeightKg,
    bool IsHazardous);
