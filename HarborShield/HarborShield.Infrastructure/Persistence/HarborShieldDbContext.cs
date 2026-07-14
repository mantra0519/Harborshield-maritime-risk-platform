using HarborShield.Application.Common.Interfaces;
using HarborShield.Domain.Cargo;
using HarborShield.Domain.Copilot;
using HarborShield.Domain.Idempotency;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using HarborShield.Domain.Webhooks;
using HarborShield.Domain.Zones;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Infrastructure.Persistence;

public class HarborShieldDbContext : DbContext, IApplicationDbContext
{
    public HarborShieldDbContext(DbContextOptions<HarborShieldDbContext> options) : base(options)
    {
    }

    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<VesselPositionEvent> VesselPositionEvents => Set<VesselPositionEvent>();
    public DbSet<CargoManifest> CargoManifests => Set<CargoManifest>();
    public DbSet<RiskCase> RiskCases => Set<RiskCase>();
    public DbSet<RestrictedZone> RestrictedZones => Set<RestrictedZone>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<RiskCaseEmbedding> RiskCaseEmbeddings => Set<RiskCaseEmbedding>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HarborShieldDbContext).Assembly);
    }
}
