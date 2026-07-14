using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Cargo;
using HarborShield.Domain.Cargo;
using HarborShield.Domain.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Cargo.Commands.SubmitCargoManifest;

public class SubmitCargoManifestCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SubmitCargoManifestCommand, CargoManifestResponse>
{
    public async ValueTask<CargoManifestResponse> Handle(SubmitCargoManifestCommand request, CancellationToken cancellationToken)
    {
        var vesselExists = await db.Vessels.AnyAsync(v => v.Id == request.VesselId, cancellationToken);
        if (!vesselExists)
            throw new NotFoundException(nameof(Vessel), request.VesselId);

        var manifest = CargoManifest.Create(
            request.VesselId,
            request.OriginPort,
            request.DestinationPort,
            request.ShipperName,
            request.ReceiverName,
            request.DeclaredWeightKg,
            request.IsHazardous);

        db.CargoManifests.Add(manifest);
        await db.SaveChangesAsync(cancellationToken);

        return new CargoManifestResponse(
            manifest.Id,
            manifest.VesselId,
            manifest.OriginPort,
            manifest.DestinationPort,
            manifest.ShipperName,
            manifest.ReceiverName,
            manifest.DeclaredWeightKg,
            manifest.IsHazardous,
            manifest.SubmittedAt);
    }
}
