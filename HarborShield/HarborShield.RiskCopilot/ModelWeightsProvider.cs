using LLama;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.Configuration;

namespace HarborShield.RiskCopilot;

/// <summary>
/// Loads the local GGUF model weights once, lazily, on first use, and reuses them for every
/// subsequent embedding/generation call. Loading weights from disk is the expensive part;
/// creating a context/embedder from already-loaded weights is cheap, so this is registered
/// as a singleton.
/// </summary>
public class ModelWeightsProvider
{
    private readonly Lazy<Task<LLamaWeights>> _generationWeights;
    private readonly Lazy<Task<LLamaWeights>> _embeddingWeights;

    public ModelWeightsProvider(IConfiguration configuration)
    {
        var generationModelPath = configuration["RiskCopilot:GenerationModelPath"]
            ?? throw new InvalidOperationException("Configuration 'RiskCopilot:GenerationModelPath' is not set.");
        var embeddingModelPath = configuration["RiskCopilot:EmbeddingModelPath"]
            ?? throw new InvalidOperationException("Configuration 'RiskCopilot:EmbeddingModelPath' is not set.");

        GenerationParams = new ModelParams(generationModelPath) { ContextSize = 2048 };
        EmbeddingParams = new ModelParams(embeddingModelPath) { PoolingType = LLamaPoolingType.Mean };

        _generationWeights = new Lazy<Task<LLamaWeights>>(() => LLamaWeights.LoadFromFileAsync(GenerationParams));
        _embeddingWeights = new Lazy<Task<LLamaWeights>>(() => LLamaWeights.LoadFromFileAsync(EmbeddingParams));
    }

    public ModelParams GenerationParams { get; }
    public ModelParams EmbeddingParams { get; }

    public Task<LLamaWeights> GetGenerationWeightsAsync() => _generationWeights.Value;
    public Task<LLamaWeights> GetEmbeddingWeightsAsync() => _embeddingWeights.Value;
}
