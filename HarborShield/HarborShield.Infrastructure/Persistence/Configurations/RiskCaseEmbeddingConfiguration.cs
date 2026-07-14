using HarborShield.Domain.Copilot;
using HarborShield.Domain.RiskCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class RiskCaseEmbeddingConfiguration : IEntityTypeConfiguration<RiskCaseEmbedding>
{
    // bge-small-en-v1.5 produces 384-dimensional embeddings.
    public const int EmbeddingDimensions = 384;

    public void Configure(EntityTypeBuilder<RiskCaseEmbedding> builder)
    {
        builder.ToTable("risk_case_embeddings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Embedding).HasColumnType($"vector({EmbeddingDimensions})").IsRequired();

        builder.HasOne<RiskCase>()
            .WithMany()
            .HasForeignKey(e => e.RiskCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.RiskCaseId).IsUnique();
    }
}
