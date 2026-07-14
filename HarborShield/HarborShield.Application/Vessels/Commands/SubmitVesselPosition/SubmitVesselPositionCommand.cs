using Mediator;

namespace HarborShield.Application.Vessels.Commands.SubmitVesselPosition;

public record SubmitVesselPositionCommand(
    Guid VesselId,
    double Latitude,
    double Longitude,
    double SpeedKnots,
    double HeadingDegrees,
    string? Destination,
    DateTimeOffset RecordedAt) : IRequest<Guid>;
