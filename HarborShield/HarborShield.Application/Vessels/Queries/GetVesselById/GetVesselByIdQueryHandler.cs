using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Vessels;
using HarborShield.Domain.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Vessels.Queries.GetVesselById;

public class GetVesselByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetVesselByIdQuery, VesselResponse>
{
    public async ValueTask<VesselResponse> Handle(GetVesselByIdQuery request, CancellationToken cancellationToken)
    {
        var vessel = await db.Vessels
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VesselId, cancellationToken);

        if (vessel is null)
            throw new NotFoundException(nameof(Vessel), request.VesselId);

        return new VesselResponse(vessel.Id, vessel.ImoNumber, vessel.Name, vessel.FlagCountry, vessel.CreatedAt);
    }
}
