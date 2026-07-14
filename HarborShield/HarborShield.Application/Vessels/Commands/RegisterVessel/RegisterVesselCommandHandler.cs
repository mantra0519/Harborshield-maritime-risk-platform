using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Vessels;
using HarborShield.Domain.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Vessels.Commands.RegisterVessel;

public class RegisterVesselCommandHandler(IApplicationDbContext db) : IRequestHandler<RegisterVesselCommand, VesselResponse>
{
    public async ValueTask<VesselResponse> Handle(RegisterVesselCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await db.Vessels
            .AnyAsync(v => v.ImoNumber == request.ImoNumber, cancellationToken);

        if (alreadyExists)
            throw new ConflictException($"A vessel with IMO number '{request.ImoNumber}' is already registered.");

        var vessel = Vessel.Create(request.ImoNumber, request.Name, request.FlagCountry);

        db.Vessels.Add(vessel);
        await db.SaveChangesAsync(cancellationToken);

        return new VesselResponse(vessel.Id, vessel.ImoNumber, vessel.Name, vessel.FlagCountry, vessel.CreatedAt);
    }
}
