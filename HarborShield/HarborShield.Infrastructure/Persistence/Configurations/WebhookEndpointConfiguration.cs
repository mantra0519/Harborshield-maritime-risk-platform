using HarborShield.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HarborShield.Infrastructure.Persistence.Configurations;

public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("webhook_endpoints");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Url).HasMaxLength(2000).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(200).IsRequired();
        builder.Property(w => w.SubscribedEventTypes).HasColumnType("text[]");
    }
}
