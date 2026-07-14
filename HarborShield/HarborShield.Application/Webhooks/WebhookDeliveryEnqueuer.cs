using System.Text.Json;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Application.Webhooks;

/// <summary>
/// Adds pending WebhookDelivery rows for every endpoint subscribed to the given event type.
/// Doesn't call SaveChangesAsync itself - the caller's existing save covers it, so the risk
/// case (or its resolution) and its outgoing webhook jobs commit atomically.
/// </summary>
public class WebhookDeliveryEnqueuer(IApplicationDbContext db)
{
    public async Task EnqueueAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        var activeEndpoints = await db.WebhookEndpoints
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);

        var subscribed = activeEndpoints.Where(e => e.IsSubscribedTo(eventType)).ToList();
        if (subscribed.Count == 0)
            return;

        var payloadJson = JsonSerializer.Serialize(payload);

        foreach (var endpoint in subscribed)
        {
            db.WebhookDeliveries.Add(WebhookDelivery.Create(endpoint.Id, eventType, payloadJson));
        }
    }
}
