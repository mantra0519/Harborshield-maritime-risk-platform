using HarborShield.Application.RiskCases.Copilot;
using LLama;

namespace HarborShield.RiskCopilot;

public class LlamaEmbeddingService(ModelWeightsProvider modelProvider) : IEmbeddingService
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var weights = await modelProvider.GetEmbeddingWeightsAsync();
        using var embedder = new LLamaEmbedder(weights, modelProvider.EmbeddingParams);

        var embeddings = await embedder.GetEmbeddings(text);
        return embeddings.Single().ToArray();
    }
}
