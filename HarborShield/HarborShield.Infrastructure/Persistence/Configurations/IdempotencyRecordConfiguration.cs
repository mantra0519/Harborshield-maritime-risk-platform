using HarborShield.Domain.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ResponseBody).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(r => new { r.Key, r.Path }).IsUnique();
    }
}
