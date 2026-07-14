namespace HarborShield.Domain.Cargo;

public class CargoManifest
{
    public Guid Id { get; private set; }
    public Guid VesselId { get; private set; }
    public string OriginPort { get; private set; } = default!;
    public string DestinationPort { get; private set; } = default!;
    public string ShipperName { get; private set; } = default!;
    public string ReceiverName { get; private set; } = default!;
    public double DeclaredWeightKg { get; private set; }
    public bool IsHazardous { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    private CargoManifest()
    {
    }

    public static CargoManifest Create(
        Guid vesselId,
        string originPort,
        string destinationPort,
        string shipperName,
        string receiverName,
        double declaredWeightKg,
        bool isHazardous)
    {
        if (string.IsNullOrWhiteSpace(originPort))
            throw new ArgumentException("Origin port is required.", nameof(originPort));
        if (string.IsNullOrWhiteSpace(destinationPort))
            throw new ArgumentException("Destination port is required.", nameof(destinationPort));
        if (declaredWeightKg < 0)
            throw new ArgumentOutOfRangeException(nameof(declaredWeightKg), "Declared weight cannot be negative.");

        return new CargoManifest
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            OriginPort = originPort,
            DestinationPort = destinationPort,
            ShipperName = shipperName,
            ReceiverName = receiverName,
            DeclaredWeightKg = declaredWeightKg,
            IsHazardous = isHazardous,
            SubmittedAt = DateTimeOffset.UtcNow
        };
    }
}
