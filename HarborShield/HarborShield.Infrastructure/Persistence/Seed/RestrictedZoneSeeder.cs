using HarborShield.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HarborShield.Infrastructure.Persistence.Seed;

public static class RestrictedZoneSeeder
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static async Task SeedAsync(HarborShieldDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.RestrictedZones.AnyAsync(cancellationToken))
            return;

        var gulfOfAden = GeometryFactory.CreatePolygon(
        [
            new Coordinate(43, 11),
            new Coordinate(49, 11),
            new Coordinate(49, 14.5),
            new Coordinate(43, 14.5),
            new Coordinate(43, 11)
        ]);

        db.RestrictedZones.Add(RestrictedZone.Create("Gulf of Aden High-Risk Corridor", gulfOfAden));

        await db.SaveChangesAsync(cancellationToken);
    }
}
