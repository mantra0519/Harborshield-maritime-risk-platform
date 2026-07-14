using System.Text;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.RiskCases.Copilot;
using HarborShield.Domain.RiskCases;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace HarborShield.RiskCopilot;

public class LlamaRiskCaseExplainer(
    IApplicationDbContext db,
    IEmbeddingService embeddingService,
    ModelWeightsProvider modelProvider,
    ILogger<LlamaRiskCaseExplainer> logger) : IRiskCaseExplainer
{
    private const int SimilarCaseCount = 3;

    public async Task<string> ExplainAsync(Guid riskCaseId, CancellationToken cancellationToken)
    {
        var riskCase = await db.RiskCases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == riskCaseId, cancellationToken)
            ?? throw new InvalidOperationException($"Risk case '{riskCaseId}' was not found.");

        var queryVector = await GetOrComputeEmbeddingAsync(riskCase, cancellationToken);

        var similarCases = await db.RiskCaseEmbeddings
            .Where(e => e.RiskCaseId != riskCaseId)
            .OrderBy(e => e.Embedding.CosineDistance(queryVector))
            .Take(SimilarCaseCount)
            .Join(db.RiskCases, e => e.RiskCaseId, r => r.Id, (e, r) => r)
            .ToListAsync(cancellationToken);

        var prompt = BuildPrompt(riskCase, similarCases);

        return await GenerateAsync(prompt, cancellationToken);
    }

    private async Task<Vector> GetOrComputeEmbeddingAsync(RiskCase riskCase, CancellationToken cancellationToken)
    {
        var existing = await db.RiskCaseEmbeddings
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.RiskCaseId == riskCase.Id, cancellationToken);

        if (existing is not null)
            return existing.Embedding;

        // Not embedded yet (the background worker hasn't gotten to it) - compute on demand
        // so Explain always works instead of failing on brand-new cases.
        var floats = await embeddingService.EmbedAsync(BuildCaseText(riskCase), cancellationToken);
        return new Vector(floats);
    }

    private static string BuildCaseText(RiskCase riskCase) =>
        $"{riskCase.CaseType} (severity {riskCase.Severity}, score {riskCase.RiskScore}): {string.Join("; ", riskCase.Reasons)}";

    private static string BuildPrompt(RiskCase current, IReadOnlyList<RiskCase> similarCases)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "You are a maritime risk analyst assistant. Explain why this risk case was flagged, " +
            "in 3-5 concise sentences for a human investigator. Mention the most likely cause and " +
            "whether similar cases have occurred before.");
        sb.AppendLine();
        sb.AppendLine("Current case:");
        sb.AppendLine($"- Type: {current.CaseType}");
        sb.AppendLine($"- Severity: {current.Severity}");
        sb.AppendLine($"- Risk score: {current.RiskScore}");
        sb.AppendLine($"- Reasons: {string.Join("; ", current.Reasons)}");
        sb.AppendLine();

        if (similarCases.Count == 0)
        {
            sb.AppendLine("No similar historical cases were found.");
        }
        else
        {
            sb.AppendLine("Similar historical cases:");
            foreach (var similar in similarCases)
            {
                sb.AppendLine($"- {similar.CaseType} (severity {similar.Severity}): {string.Join("; ", similar.Reasons)}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var weights = await modelProvider.GetGenerationWeightsAsync();
        using var context = weights.CreateContext(modelProvider.GenerationParams);
        var executor = new InteractiveExecutor(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, "You are a concise, factual maritime risk analyst.");

        var session = new ChatSession(executor, chatHistory);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = 150,
            // Deliberately does NOT include "<|assistant|>" - the model emits that as its very
            // first token (a template artifact), so treating it as a stop condition would halt
            // generation before any real content. CleanUpResponse strips it as a leading marker
            // instead, and only treats it as a cutoff if it reappears later mid-response.
            AntiPrompts = ["User:", "System:", "<|user|>", "<|system|>", "<|end|>", "**Assistant"],
            SamplingPipeline = new DefaultSamplingPipeline
            {
                RepeatPenalty = 1.3f
            }
        };

        var responseBuilder = new StringBuilder();

        await foreach (var token in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), inferenceParams)
            .WithCancellation(cancellationToken))
        {
            responseBuilder.Append(token);
        }

        var cleaned = CleanUpResponse(responseBuilder.ToString());

        if (string.IsNullOrWhiteSpace(cleaned))
            logger.LogWarning("Cleaned explanation was empty. Raw model output: {RawResponse}", responseBuilder.ToString());

        return cleaned;
    }

    /// <summary>
    /// Belt-and-suspenders on top of AntiPrompts: small instruct models sometimes echo their
    /// own role marker at the very start of the continuation, then loop rephrasing the same
    /// answer under repeated "new turn" markers. Strip a leading marker if present, then cut
    /// at the next one - that keeps the first real answer and discards everything after.
    /// </summary>
    private static readonly string[] TurnMarkers =
        ["<|assistant|>", "<|user|>", "<|system|>", "<|end|>", "**Assistant", "Assistant:", "System:", "User:"];

    private static string CleanUpResponse(string response)
    {
        var text = response.TrimStart();

        foreach (var marker in TurnMarkers)
        {
            if (text.StartsWith(marker, StringComparison.Ordinal))
            {
                text = text[marker.Length..].TrimStart(':', ' ', '*', '\r', '\n');
                break;
            }
        }

        var cutIndex = text.Length;

        foreach (var marker in TurnMarkers)
        {
            var index = text.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0 && index < cutIndex)
                cutIndex = index;
        }

        text = text[..cutIndex].Trim().TrimEnd('*').Trim();

        // MaxTokens sometimes cuts off mid-ramble into a second, redundant attempt at the same
        // answer, separated by a blank line or a "===" divider. Keep just the first paragraph.
        var firstParagraph = text
            .Split(["\n\n", "\n===", "\n---"], StringSplitOptions.None)
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

        return (firstParagraph ?? text).Trim();
    }
}
