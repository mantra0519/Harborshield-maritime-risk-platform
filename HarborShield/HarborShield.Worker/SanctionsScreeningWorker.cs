using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.RiskCases.Commands.CreateRiskCase;
using HarborShield.Application.Vessels.Sanctions;
using HarborShield.Domain.RiskCases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Worker;

/// <summary>
/// Polls newly-registered vessels and screens them against the sanctions/ownership vendor.
/// Runs independently of anomaly detection since it's driven by vessel registration, not
/// position events.
/// </summary>
public class SanctionsScreeningWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SanctionsScreeningWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScreenUnscreenedVesselsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while screening vessels against the sanctions vendor.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ScreenUnscreenedVesselsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sanctionsClient = scope.ServiceProvider.GetRequiredService<ISanctionsScreeningClient>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var unscreened = await db.Vessels
            .Where(v => v.ScreenedAt == null)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var vessel in unscreened)
        {
            SanctionsScreeningResult result;
            try
            {
                result = await sanctionsClient.ScreenAsync(
                    new SanctionsScreeningRequest(vessel.Name, vessel.FlagCountry),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Leave ScreenedAt null so this vessel is retried next cycle instead of
                // silently skipped, and don't let one vendor outage kill the whole batch.
                logger.LogWarning(ex, "Sanctions vendor unavailable for vessel {VesselId}; will retry next cycle.", vessel.Id);
                continue;
            }

            if (result.IsMatch)
            {
                var command = new CreateRiskCaseCommand(
                    vessel.Id,
                    RiskCaseType.SanctionsMatch,
                    RiskSeverity.Critical,
                    90,
                    [$"Vessel name matched watchlist entry '{result.MatchedEntity}' with {result.Confidence:P0} confidence"]);

                var riskCase = await mediator.Send(command, cancellationToken);

                logger.LogWarning(
                    "Created SanctionsMatch risk case {RiskCaseId} for vessel {VesselId}: {MatchedEntity}",
                    riskCase.Id,
                    vessel.Id,
                    result.MatchedEntity);
            }

            vessel.MarkScreened();
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
