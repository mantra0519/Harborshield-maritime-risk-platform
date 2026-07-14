using HarborShield.Application.Common.Interfaces;
using HarborShield.Contracts.Vessels;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Vessels.Queries.ListVessels;

public class ListVesselsQueryHandler(IApplicationDbContext db) : IRequestHandler<ListVesselsQuery, IReadOnlyList<VesselResponse>>
{
    public async ValueTask<IReadOnlyList<VesselResponse>> Handle(ListVesselsQuery request, CancellationToken cancellationToken)
    {
        var vessels = await db.Vessels
            .AsNoTracking()
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        return vessels
            .Select(v => new VesselResponse(v.Id, v.ImoNumber, v.Name, v.FlagCountry, v.CreatedAt))
            .ToList();
    }
}
