using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class RiskCaseConfiguration : IEntityTypeConfiguration<RiskCase>
{
    public void Configure(EntityTypeBuilder<RiskCase> builder)
    {
        builder.ToTable("risk_cases");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CaseType).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Reasons).HasColumnType("text[]");
        builder.Property(r => r.CachedExplanation).HasColumnType("text");

        builder.HasOne<Vessel>()
            .WithMany()
            .HasForeignKey(r => r.VesselId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.Status);
    }
}
