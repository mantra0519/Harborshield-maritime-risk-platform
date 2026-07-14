using System.Text;
using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.Common.Security;
using HarborShield.Domain.Webhooks;
using HarborShield.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Worker;

/// <summary>
/// Polls pending webhook deliveries and POSTs each as an HMAC-SHA256-signed payload to its
/// registered customer endpoint. Retry/timeout is handled by the "WebhookDelivery" resilience
/// pipeline; deliveries that keep failing past WebhookDelivery.MaxAttempts stop being retried.
/// </summary>
public class WebhookDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverPendingWebhooksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while delivering webhooks.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DeliverPendingWebhooksAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var pending = await db.WebhookDeliveries
            .Where(d => d.Status == WebhookDeliveryStatus.Pending)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var endpointIds = pending.Select(d => d.WebhookEndpointId).Distinct().ToList();
        var endpoints = await db.WebhookEndpoints
            .Where(w => endpointIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var httpClient = httpClientFactory.CreateClient(WebhookDeliveryHttpClientName.Name);

        foreach (var delivery in pending)
        {
            if (!endpoints.TryGetValue(delivery.WebhookEndpointId, out var endpoint) || !endpoint.IsActive)
            {
                delivery.RecordFailedAttempt("Webhook endpoint no longer exists or is inactive.");
                continue;
            }

            var signature = HmacSigner.ComputeSignature(endpoint.Secret, delivery.PayloadJson);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(delivery.PayloadJson, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("X-HarborShield-Signature", signature);
            httpRequest.Headers.Add("X-HarborShield-Event", delivery.EventType);
            httpRequest.Headers.Add("X-HarborShield-Delivery-Id", delivery.Id.ToString());

            try
            {
                var response = await httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    delivery.MarkDelivered();
                    logger.LogInformation(
                        "Delivered webhook {DeliveryId} ({EventType}) to {CustomerName}",
                        delivery.Id, delivery.EventType, endpoint.CustomerName);
                }
                else
                {
                    delivery.RecordFailedAttempt($"Endpoint responded with {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                delivery.RecordFailedAttempt(ex.Message);
                logger.LogWarning(ex, "Failed to deliver webhook {DeliveryId} to {CustomerName}", delivery.Id, endpoint.CustomerName);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
