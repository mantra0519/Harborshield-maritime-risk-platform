namespace HarborShield.Contracts.Vessels;

public record SubmitVesselPositionRequest(
    double Latitude,
    double Longitude,
    double SpeedKnots,
    double HeadingDegrees,
    string? Destination,
    DateTimeOffset RecordedAt);
