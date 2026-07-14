using HarborShield.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EventType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<WebhookEndpoint>()
            .WithMany()
            .HasForeignKey(d => d.WebhookEndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.Status);
    }
}
