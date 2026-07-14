using Pgvector;

namespace HarborShield.Domain.Copilot;

public class RiskCaseEmbedding
{
    public Guid Id { get; private set; }
    public Guid RiskCaseId { get; private set; }
    public Vector Embedding { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private RiskCaseEmbedding()
    {
    }

    public static RiskCaseEmbedding Create(Guid riskCaseId, Vector embedding)
    {
        return new RiskCaseEmbedding
        {
            Id = Guid.NewGuid(),
            RiskCaseId = riskCaseId,
            Embedding = embedding,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
