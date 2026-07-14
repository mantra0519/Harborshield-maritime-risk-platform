using HarborShield.Application.RiskCases.Copilot;
using HarborShield.Domain.Cargo;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HarborShield.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a handful of realistic vessels/risk cases so a freshly-deployed instance has content
/// to look at instead of an empty database. Explanations for these cases are generated with
/// the real local LLM once, here, and cached on the entity - so viewing them is instant even
/// though nothing is precomputed for cases created later through the API (those run live).
/// One vessel ("MV Shadow Runner") is deliberately left unscreened so the real
/// SanctionsScreeningWorker discovers and flags it through the actual vendor-call pipeline
/// shortly after startup, rather than being faked here.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        HarborShieldDbContext db,
        IRiskCaseExplainer explainer,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.Vessels.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var cleanVessel = Vessel.Create("9074729", "MV Northern Horizon", "Panama");
        var routeDeviationVessel = Vessel.Create("9321483", "MV Aegean Voyager", "Malta");
        var restrictedZoneVessel = Vessel.Create("9456721", "MV Crimson Tide", "Liberia");
        var trackingGapVessel = Vessel.Create("9587234", "MV Silver Compass", "Marshall Islands");
        var sanctionsVessel = Vessel.Create("9612845", "MV Shadow Runner", "Panama");

        db.Vessels.AddRange(cleanVessel, routeDeviationVessel, restrictedZoneVessel, trackingGapVessel, sanctionsVessel);

        cleanVessel.MarkScreened();
        routeDeviationVessel.MarkScreened();
        restrictedZoneVessel.MarkScreened();
        trackingGapVessel.MarkScreened();
        // sanctionsVessel left unscreened on purpose - see summary above.

        var cleanPrev = VesselPositionEvent.Create(cleanVessel.Id, 25.2, 55.3, 14.0, 90, "Jebel Ali", now.AddHours(-12));
        var cleanCurrent = VesselPositionEvent.Create(cleanVessel.Id, 25.6, 56.1, 13.5, 88, "Jebel Ali", now.AddHours(-6));

        var routePrev = VesselPositionEvent.Create(routeDeviationVessel.Id, 35.9, 14.5, 18.0, 95, "Alexandria", now.AddHours(-6));
        var routeCurrent = VesselPositionEvent.Create(routeDeviationVessel.Id, 35.3, 25.1, 18.0, 100, "Alexandria", now);

        var zonePosition = VesselPositionEvent.Create(restrictedZoneVessel.Id, 12.5, 46.0, 11.0, 270, "Djibouti", now.AddHours(-1));

        var gapPrev = VesselPositionEvent.Create(trackingGapVessel.Id, 1.3, 103.8, 12.0, 45, "Singapore", now.AddHours(-24));
        var gapCurrent = VesselPositionEvent.Create(trackingGapVessel.Id, 1.5, 104.5, 12.5, 50, "Singapore", now.AddHours(-6));

        var sanctionsPosition = VesselPositionEvent.Create(sanctionsVessel.Id, 33.0, -118.3, 15.0, 180, "Los Angeles", now.AddHours(-3));

        db.VesselPositionEvents.AddRange(
            cleanPrev, cleanCurrent, routePrev, routeCurrent, zonePosition, gapPrev, gapCurrent, sanctionsPosition);

        foreach (var position in new[] { cleanPrev, cleanCurrent, routePrev, routeCurrent, zonePosition, gapPrev, gapCurrent, sanctionsPosition })
            position.MarkProcessed();

        var cargoManifest = CargoManifest.Create(
            routeDeviationVessel.Id,
            "Piraeus, Greece",
            "Alexandria, Egypt",
            "Aegean Bulk Traders Ltd",
            "Nile Import Co.",
            declaredWeightKg: 45000,
            isHazardous: true);

        db.CargoManifests.Add(cargoManifest);

        var routeDeviationCase = RiskCase.Create(
            routeDeviationVessel.Id,
            RiskCaseType.RouteDeviation,
            RiskSeverity.High,
            80,
            ["Vessel moved 960 km in 6.0 hours, implying 86.4 knots versus reported 18.0 knots"]);

        var restrictedZoneCase = RiskCase.Create(
            restrictedZoneVessel.Id,
            RiskCaseType.RestrictedZoneEntry,
            RiskSeverity.Critical,
            95,
            ["Vessel entered restricted zone 'Gulf of Aden High-Risk Corridor'"]);

        var trackingGapCase = RiskCase.Create(
            trackingGapVessel.Id,
            RiskCaseType.TrackingGap,
            RiskSeverity.High,
            76,
            ["Tracking signal unavailable for 18.0 hours"]);
        trackingGapCase.Acknowledge();
        trackingGapCase.Resolve("Confirmed with port authority: AIS transponder was offline for scheduled maintenance, not evasion.");

        var cargoAnomalyCase = RiskCase.Create(
            routeDeviationVessel.Id,
            RiskCaseType.CargoAnomaly,
            RiskSeverity.Medium,
            55,
            [
                "Cargo manifest declares hazardous materials (industrial solvents) with no hazardous-cargo endorsement on file",
                "Declared weight of 45000 kg is inconsistent with vessel's registered cargo capacity for this route"
            ]);

        db.RiskCases.AddRange(routeDeviationCase, restrictedZoneCase, trackingGapCase, cargoAnomalyCase);

        await db.SaveChangesAsync(cancellationToken);

        foreach (var riskCase in new[] { routeDeviationCase, restrictedZoneCase, trackingGapCase, cargoAnomalyCase })
        {
            try
            {
                var explanation = await explainer.ExplainAsync(riskCase.Id, cancellationToken);
                riskCase.CacheExplanation(explanation);
                logger.LogInformation("Pre-generated demo explanation for risk case {RiskCaseId}", riskCase.Id);
            }
            catch (Exception ex)
            {
                // Demo data seeding shouldn't block startup if the model isn't available yet -
                // the explain endpoint falls back to live generation on first request either way.
                logger.LogWarning(ex, "Could not pre-generate demo explanation for risk case {RiskCaseId}; it will generate on first request instead.", riskCase.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
