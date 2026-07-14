using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Domain.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Vessels.Commands.SubmitVesselPosition;

public class SubmitVesselPositionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SubmitVesselPositionCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitVesselPositionCommand request, CancellationToken cancellationToken)
    {
        var vesselExists = await db.Vessels.AnyAsync(v => v.Id == request.VesselId, cancellationToken);
        if (!vesselExists)
            throw new NotFoundException(nameof(Vessel), request.VesselId);

        var positionEvent = VesselPositionEvent.Create(
            request.VesselId,
            request.Latitude,
            request.Longitude,
            request.SpeedKnots,
            request.HeadingDegrees,
            request.Destination,
            request.RecordedAt);

        db.VesselPositionEvents.Add(positionEvent);
        await db.SaveChangesAsync(cancellationToken);

        return positionEvent.Id;
    }
}
