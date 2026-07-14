namespace HarborShield.Contracts.Webhooks;

public record WebhookEndpointResponse(
    Guid Id,
    string CustomerName,
    string Url,
    IReadOnlyList<string> SubscribedEventTypes,
    bool IsActive,
    DateTimeOffset CreatedAt);
