using NetTopologySuite.Geometries;

namespace HarborShield.Domain.Zones;

public class RestrictedZone
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Polygon Area { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private RestrictedZone()
    {
    }

    public static RestrictedZone Create(string name, Polygon area)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zone name is required.", nameof(name));

        return new RestrictedZone
        {
            Id = Guid.NewGuid(),
            Name = name,
            Area = area,
            IsActive = true
        };
    }
}
