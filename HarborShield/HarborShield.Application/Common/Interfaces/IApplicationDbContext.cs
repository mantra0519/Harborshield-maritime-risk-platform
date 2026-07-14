using HarborShield.Domain.Cargo;
using HarborShield.Domain.Copilot;
using HarborShield.Domain.Idempotency;
using HarborShield.Domain.RiskCases;
using HarborShield.Domain.Vessels;
using HarborShield.Domain.Webhooks;
using HarborShield.Domain.Zones;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Vessel> Vessels { get; }
    DbSet<VesselPositionEvent> VesselPositionEvents { get; }
    DbSet<CargoManifest> CargoManifests { get; }
    DbSet<RiskCase> RiskCases { get; }
    DbSet<RestrictedZone> RestrictedZones { get; }
    DbSet<WebhookEndpoint> WebhookEndpoints { get; }
    DbSet<WebhookDelivery> WebhookDeliveries { get; }
    DbSet<RiskCaseEmbedding> RiskCaseEmbeddings { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
