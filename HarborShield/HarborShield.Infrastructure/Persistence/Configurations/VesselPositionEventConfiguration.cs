using HarborShield.Domain.Vessels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class VesselPositionEventConfiguration : IEntityTypeConfiguration<VesselPositionEvent>
{
    public void Configure(EntityTypeBuilder<VesselPositionEvent> builder)
    {
        builder.ToTable("vessel_position_events");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Position)
            .HasColumnType("geography (Point, 4326)")
            .IsRequired();

        builder.HasIndex(p => p.Position).HasMethod("GIST");
        builder.HasIndex(p => new { p.VesselId, p.RecordedAt });

        builder.HasOne<Vessel>()
            .WithMany()
            .HasForeignKey(p => p.VesselId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
