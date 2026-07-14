using HarborShield.Application.Common.Geo;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using HarborShield.Domain.Zones;

namespace HarborShield.Application.Vessels.AnomalyDetection;

/// <summary>
/// Pure rule-based checks. No I/O - callers load the current/previous position and active
/// zones, and this just decides what (if anything) looks wrong.
/// </summary>
public class VesselAnomalyDetector
{
    public IReadOnlyList<DetectedAnomaly> Detect(
        VesselPositionEvent current,
        VesselPositionEvent? previous,
        IReadOnlyList<RestrictedZone> activeZones)
    {
        var anomalies = new List<DetectedAnomaly>();

        var zoneReasons = activeZones
            .Where(z => z.Area.Contains(current.Position))
            .Select(z => $"Vessel entered restricted zone '{z.Name}'")
            .ToList();

        if (zoneReasons.Count > 0)
            anomalies.Add(new DetectedAnomaly(RiskCaseType.RestrictedZoneEntry, RiskSeverity.Critical, 95, zoneReasons));

        if (previous is not null)
        {
            var gap = current.RecordedAt - previous.RecordedAt;

            if (gap > VesselAnomalyThresholds.TrackingGapThreshold)
            {
                var severity = gap.TotalHours > 12 ? RiskSeverity.High : RiskSeverity.Medium;
                var riskScore = Math.Min(90, 40 + (int)gap.TotalHours * 2);

                anomalies.Add(new DetectedAnomaly(
                    RiskCaseType.TrackingGap,
                    severity,
                    riskScore,
                    [$"Tracking signal unavailable for {gap.TotalHours:F1} hours"]));
            }

            var routeReasons = new List<string>();

            if (current.SpeedKnots > VesselAnomalyThresholds.MaxPlausibleSpeedKnots)
                routeReasons.Add($"Reported speed {current.SpeedKnots:F1} knots exceeds plausible maximum");

            var distanceKm = GeoDistanceCalculator.HaversineKm(
                previous.Position.Y, previous.Position.X,
                current.Position.Y, current.Position.X);

            var elapsedHours = gap.TotalHours;

            if (elapsedHours > 0 && distanceKm > VesselAnomalyThresholds.MinDistanceForSpeedCheckKm)
            {
                var impliedSpeedKnots = distanceKm / elapsedHours * 0.539957;

                if (impliedSpeedKnots > current.SpeedKnots * VesselAnomalyThresholds.ImpliedSpeedMultiplierThreshold
                    && impliedSpeedKnots > VesselAnomalyThresholds.MinImpliedSpeedKnotsToFlag)
                {
                    routeReasons.Add(
                        $"Vessel moved {distanceKm:F0} km in {elapsedHours:F1} hours, implying {impliedSpeedKnots:F1} knots versus reported {current.SpeedKnots:F1} knots");
                }
            }

            if (routeReasons.Count > 0)
                anomalies.Add(new DetectedAnomaly(RiskCaseType.RouteDeviation, RiskSeverity.High, 80, routeReasons));
        }

        return anomalies;
    }
}
