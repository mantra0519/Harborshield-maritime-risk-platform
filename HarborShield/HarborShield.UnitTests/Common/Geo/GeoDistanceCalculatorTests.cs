using FluentAssertions;
using HarborShield.Application.Common.Geo;

namespace HarborShield.UnitTests.Common.Geo;

public class GeoDistanceCalculatorTests
{
    [Fact]
    public void HaversineKm_SamePoint_ReturnsZero()
    {
        var distance = GeoDistanceCalculator.HaversineKm(9.05, -79.68, 9.05, -79.68);

        distance.Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void HaversineKm_KnownDistance_MatchesExpectedWithinTolerance()
    {
        // Panama City (~9.0, -79.5) to a point roughly 111km due north (1 degree latitude).
        var distance = GeoDistanceCalculator.HaversineKm(9.0, -79.5, 10.0, -79.5);

        distance.Should().BeApproximately(111.0, 1.0);
    }

    [Fact]
    public void HaversineKm_AntipodalPoints_ReturnsHalfEarthCircumference()
    {
        var distance = GeoDistanceCalculator.HaversineKm(0, 0, 0, 180);

        distance.Should().BeApproximately(20015.0, 5.0);
    }
}
