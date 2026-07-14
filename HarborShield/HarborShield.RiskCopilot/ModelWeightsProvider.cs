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
    private readonly object _generationWeightsLock = new();
    private Lazy<Task<LLamaWeights>> _generationWeights;
    private readonly Lazy<Task<LLamaWeights>> _embeddingWeights;

    public ModelWeightsProvider(IConfiguration configuration)
    {
        var generationModelPath = configuration["RiskCopilot:GenerationModelPath"]
            ?? throw new InvalidOperationException("Configuration 'RiskCopilot:GenerationModelPath' is not set.");
        var embeddingModelPath = configuration["RiskCopilot:EmbeddingModelPath"]
            ?? throw new InvalidOperationException("Configuration 'RiskCopilot:EmbeddingModelPath' is not set.");

        GenerationParams = new ModelParams(generationModelPath) { ContextSize = 2048 };
        EmbeddingParams = new ModelParams(embeddingModelPath) { PoolingType = LLamaPoolingType.Mean };

        _generationWeights = CreateGenerationWeightsLazy();
        _embeddingWeights = new Lazy<Task<LLamaWeights>>(() => LLamaWeights.LoadFromFileAsync(EmbeddingParams));
    }

    public ModelParams GenerationParams { get; }
    public ModelParams EmbeddingParams { get; }

    public Task<LLamaWeights> GetGenerationWeightsAsync() => _generationWeights.Value;
    public Task<LLamaWeights> GetEmbeddingWeightsAsync() => _embeddingWeights.Value;

    /// <summary>
    /// Frees the generation model's memory (~500MB+ depending on the model) if it was loaded.
    /// Intended for hosts that only need generation briefly (e.g. Worker's one-time demo-data
    /// seeding) and never again afterward - without this, the weights stay resident for the
    /// rest of the process's life even though nothing keeps using them. Safe to call even if
    /// generation was never used; a later call to GetGenerationWeightsAsync reloads from disk.
    /// </summary>
    public async Task UnloadGenerationWeightsAsync()
    {
        Lazy<Task<LLamaWeights>> previous;

        lock (_generationWeightsLock)
        {
            previous = _generationWeights;
            _generationWeights = CreateGenerationWeightsLazy();
        }

        if (!previous.IsValueCreated)
            return;

        var weights = await previous.Value;
        weights.Dispose();
    }

    private Lazy<Task<LLamaWeights>> CreateGenerationWeightsLazy() =>
        new(() => LLamaWeights.LoadFromFileAsync(GenerationParams));
}
