using HarborShield.Domain.Zones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class RestrictedZoneConfiguration : IEntityTypeConfiguration<RestrictedZone>
{
    public void Configure(EntityTypeBuilder<RestrictedZone> builder)
    {
        builder.ToTable("restricted_zones");
        builder.HasKey(z => z.Id);

        builder.Property(z => z.Name).HasMaxLength(200).IsRequired();
        builder.Property(z => z.Area).HasColumnType("geometry (Polygon, 4326)").IsRequired();
    }
}
