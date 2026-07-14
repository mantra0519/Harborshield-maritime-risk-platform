namespace HarborShield.Application.RiskCases.Copilot;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);
}
