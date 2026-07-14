using FluentAssertions;
using HarborShield.Application.Vessels.AnomalyDetection;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using HarborShield.Domain.Zones;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace HarborShield.UnitTests.Vessels.AnomalyDetection;

public class VesselAnomalyDetectorTests
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly VesselAnomalyDetector _detector = new();
    private readonly Guid _vesselId = Guid.NewGuid();

    private VesselPositionEvent Position(
        double lat, double lon, double speedKnots, DateTimeOffset recordedAt, double heading = 90) =>
        VesselPositionEvent.Create(_vesselId, lat, lon, speedKnots, heading, destination: null, recordedAt);

    [Fact]
    public void Detect_NoAnomalousConditions_ReturnsEmpty()
    {
        var baseline = DateTimeOffset.UtcNow;
        var previous = Position(9.00, -79.60, speedKnots: 12, baseline);
        var current = Position(9.02, -79.58, speedKnots: 12, baseline.AddMinutes(20));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Detect_NoPreviousPosition_OnlyRunsZoneCheck()
    {
        var current = Position(9.00, -79.60, speedKnots: 12, DateTimeOffset.UtcNow);

        var act = () => _detector.Detect(current, previous: null, activeZones: []);

        act.Should().NotThrow();
        act().Should().BeEmpty();
    }

    [Fact]
    public void Detect_PositionInsideRestrictedZone_ReturnsRestrictedZoneEntryAnomaly()
    {
        var zonePolygon = GeometryFactory.CreatePolygon(
        [
            new Coordinate(43, 11),
            new Coordinate(49, 11),
            new Coordinate(49, 14.5),
            new Coordinate(43, 14.5),
            new Coordinate(43, 11)
        ]);
        var zone = RestrictedZone.Create("Gulf of Aden High-Risk Corridor", zonePolygon);

        var current = Position(lat: 12.5, lon: 45.5, speedKnots: 10, DateTimeOffset.UtcNow);

        var result = _detector.Detect(current, previous: null, activeZones: [zone]);

        result.Should().ContainSingle(a => a.CaseType == RiskCaseType.RestrictedZoneEntry);
        result.Single().Severity.Should().Be(RiskSeverity.Critical);
        result.Single().Reasons.Should().ContainMatch("*Gulf of Aden*");
    }

    [Fact]
    public void Detect_PositionOutsideRestrictedZone_DoesNotFlagZoneEntry()
    {
        var zonePolygon = GeometryFactory.CreatePolygon(
        [
            new Coordinate(43, 11),
            new Coordinate(49, 11),
            new Coordinate(49, 14.5),
            new Coordinate(43, 14.5),
            new Coordinate(43, 11)
        ]);
        var zone = RestrictedZone.Create("Gulf of Aden High-Risk Corridor", zonePolygon);

        var current = Position(lat: 9.05, lon: -79.68, speedKnots: 10, DateTimeOffset.UtcNow);

        var result = _detector.Detect(current, previous: null, activeZones: [zone]);

        result.Should().NotContain(a => a.CaseType == RiskCaseType.RestrictedZoneEntry);
    }

    [Fact]
    public void Detect_LargeTrackingGap_ReturnsTrackingGapAnomaly()
    {
        var baseline = DateTimeOffset.UtcNow;
        var previous = Position(9.00, -79.60, speedKnots: 12, baseline);
        var current = Position(9.05, -79.55, speedKnots: 12, baseline.AddHours(6));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().ContainSingle(a => a.CaseType == RiskCaseType.TrackingGap);
    }

    [Fact]
    public void Detect_ShortGapBelowThreshold_DoesNotFlagTrackingGap()
    {
        var baseline = DateTimeOffset.UtcNow;
        var previous = Position(9.00, -79.60, speedKnots: 12, baseline);
        var current = Position(9.02, -79.58, speedKnots: 12, baseline.AddHours(1));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().NotContain(a => a.CaseType == RiskCaseType.TrackingGap);
    }

    [Fact]
    public void Detect_ImpliedSpeedFarExceedsReportedSpeed_ReturnsRouteDeviationAnomaly()
    {
        var baseline = DateTimeOffset.UtcNow;
        // Roughly 650km apart (Red Sea to Gulf of Aden) in just 10 minutes at a reported 11 knots.
        var previous = Position(15.0, 40.0, speedKnots: 11, baseline);
        var current = Position(12.5, 45.5, speedKnots: 11, baseline.AddMinutes(10));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().ContainSingle(a => a.CaseType == RiskCaseType.RouteDeviation);
        result.Single(a => a.CaseType == RiskCaseType.RouteDeviation).Reasons
            .Should().ContainMatch("*implying*knots*");
    }

    [Fact]
    public void Detect_ReportedSpeedExceedsPlausibleMaximum_ReturnsRouteDeviationAnomaly()
    {
        var baseline = DateTimeOffset.UtcNow;
        var previous = Position(9.00, -79.60, speedKnots: 12, baseline);
        var current = Position(9.001, -79.599, speedKnots: 55, baseline.AddMinutes(5));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().ContainSingle(a => a.CaseType == RiskCaseType.RouteDeviation);
        result.Single(a => a.CaseType == RiskCaseType.RouteDeviation).Reasons
            .Should().ContainMatch("*exceeds plausible maximum*");
    }

    [Fact]
    public void Detect_TinyDistanceOverShortTime_DoesNotFalselyFlagRouteDeviation()
    {
        // GPS jitter over a very small distance shouldn't trip the implied-speed check.
        var baseline = DateTimeOffset.UtcNow;
        var previous = Position(9.000000, -79.600000, speedKnots: 10, baseline);
        var current = Position(9.000050, -79.600050, speedKnots: 10, baseline.AddSeconds(30));

        var result = _detector.Detect(current, previous, activeZones: []);

        result.Should().NotContain(a => a.CaseType == RiskCaseType.RouteDeviation);
    }
}
