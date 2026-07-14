using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.RiskCases.Copilot;
using HarborShield.Domain.Copilot;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace HarborShield.Worker;

/// <summary>
/// Pre-computes embeddings for risk cases as they're created, so RiskCopilot's similarity
/// search has something to compare against without waiting on the explain request itself.
/// </summary>
public class RiskCaseEmbeddingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RiskCaseEmbeddingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EmbedUnembeddedRiskCasesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while computing risk case embeddings.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task EmbedUnembeddedRiskCasesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var embeddedIds = db.RiskCaseEmbeddings.Select(e => e.RiskCaseId);

        var unembedded = await db.RiskCases
            .Where(r => !embeddedIds.Contains(r.Id))
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (unembedded.Count == 0)
            return;

        foreach (var riskCase in unembedded)
        {
            var text = $"{riskCase.CaseType} (severity {riskCase.Severity}, score {riskCase.RiskScore}): {string.Join("; ", riskCase.Reasons)}";
            var floats = await embeddingService.EmbedAsync(text, cancellationToken);

            db.RiskCaseEmbeddings.Add(RiskCaseEmbedding.Create(riskCase.Id, new Vector(floats)));
            logger.LogInformation("Computed embedding for risk case {RiskCaseId}", riskCase.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
