using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.RiskCases.Commands.CreateRiskCase;
using HarborShield.Application.Vessels.AnomalyDetection;
using HarborShield.Domain.Zones;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Worker;

/// <summary>
/// Polls newly-ingested vessel position events, runs rule-based anomaly checks against
/// each, and creates a RiskCase (via the same Application layer the Api uses) for every
/// anomaly found.
/// </summary>
public class AnomalyDetectionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AnomalyDetectionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnprocessedPositionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing vessel position events for anomaly detection.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessUnprocessedPositionsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var detector = scope.ServiceProvider.GetRequiredService<VesselAnomalyDetector>();

        var unprocessed = await db.VesselPositionEvents
            .Where(p => p.ProcessedAt == null)
            .OrderBy(p => p.RecordedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (unprocessed.Count == 0)
            return;

        var activeZones = await db.RestrictedZones
            .Where(z => z.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var current in unprocessed)
        {
            await ProcessPositionAsync(current, db, mediator, detector, activeZones, cancellationToken);
        }
    }

    private async Task ProcessPositionAsync(
        Domain.Vessels.VesselPositionEvent current,
        IApplicationDbContext db,
        IMediator mediator,
        VesselAnomalyDetector detector,
        IReadOnlyList<RestrictedZone> activeZones,
        CancellationToken cancellationToken)
    {
        var previous = await db.VesselPositionEvents
            .Where(p => p.VesselId == current.VesselId && p.RecordedAt < current.RecordedAt)
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var anomalies = detector.Detect(current, previous, activeZones);

        foreach (var anomaly in anomalies)
        {
            var command = new CreateRiskCaseCommand(
                current.VesselId,
                anomaly.CaseType,
                anomaly.Severity,
                anomaly.RiskScore,
                anomaly.Reasons.ToList());

            var riskCase = await mediator.Send(command, cancellationToken);

            logger.LogWarning(
                "Created {CaseType} risk case {RiskCaseId} for vessel {VesselId}: {Reasons}",
                anomaly.CaseType,
                riskCase.Id,
                current.VesselId,
                string.Join("; ", anomaly.Reasons));
        }

        current.MarkProcessed();
        await db.SaveChangesAsync(cancellationToken);
    }
}
