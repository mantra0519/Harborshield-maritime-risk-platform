using HarborShield.Domain.Vessels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("vessels");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ImoNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.FlagCountry).HasMaxLength(100).IsRequired();

        builder.HasIndex(v => v.ImoNumber).IsUnique();
    }
}
