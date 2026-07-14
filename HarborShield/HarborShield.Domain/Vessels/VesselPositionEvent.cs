using NetTopologySuite.Geometries;

namespace HarborShield.Domain.Vessels;

public class VesselPositionEvent
{
    public Guid Id { get; private set; }
    public Guid VesselId { get; private set; }
    public Point Position { get; private set; } = default!;
    public double SpeedKnots { get; private set; }
    public double HeadingDegrees { get; private set; }
    public string? Destination { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    private VesselPositionEvent()
    {
    }

    public static VesselPositionEvent Create(
        Guid vesselId,
        double latitude,
        double longitude,
        double speedKnots,
        double headingDegrees,
        string? destination,
        DateTimeOffset recordedAt)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        return new VesselPositionEvent
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            Position = new Point(longitude, latitude) { SRID = 4326 },
            SpeedKnots = speedKnots,
            HeadingDegrees = headingDegrees,
            Destination = destination,
            RecordedAt = recordedAt
        };
    }

    public void MarkProcessed() => ProcessedAt = DateTimeOffset.UtcNow;
}
