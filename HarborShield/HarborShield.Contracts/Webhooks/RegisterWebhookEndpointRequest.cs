namespace HarborShield.Contracts.Webhooks;

public record RegisterWebhookEndpointRequest(
    string CustomerName,
    string Url,
    string Secret,
    List<string> SubscribedEventTypes);
