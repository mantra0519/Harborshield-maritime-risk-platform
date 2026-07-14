using HarborShield.Domain.Cargo;
using HarborShield.Domain.Vessels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class CargoManifestConfiguration : IEntityTypeConfiguration<CargoManifest>
{
    public void Configure(EntityTypeBuilder<CargoManifest> builder)
    {
        builder.ToTable("cargo_manifests");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.OriginPort).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DestinationPort).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ShipperName).HasMaxLength(200);
        builder.Property(c => c.ReceiverName).HasMaxLength(200);

        builder.HasOne<Vessel>()
            .WithMany()
            .HasForeignKey(c => c.VesselId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
