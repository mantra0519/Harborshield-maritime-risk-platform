namespace HarborShield.Domain.Webhooks;

public class WebhookDelivery
{
    public const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public Guid WebhookEndpointId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public WebhookDeliveryStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? LastError { get; private set; }

    private WebhookDelivery()
    {
    }

    public static WebhookDelivery Create(Guid webhookEndpointId, string eventType, string payloadJson)
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookEndpointId = webhookEndpointId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = WebhookDeliveryStatus.Pending,
            Attempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkDelivered()
    {
        Status = WebhookDeliveryStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailedAttempt(string error)
    {
        Attempts++;
        LastError = error;
        Status = Attempts >= MaxAttempts ? WebhookDeliveryStatus.Failed : WebhookDeliveryStatus.Pending;
    }
}
