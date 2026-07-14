namespace HarborShield.Domain.Webhooks;

public class WebhookEndpoint
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string Secret { get; private set; } = default!;
    public List<string> SubscribedEventTypes { get; private set; } = new();
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookEndpoint()
    {
    }

    public static WebhookEndpoint Create(
        string customerName,
        string url,
        string secret,
        IEnumerable<string> subscribedEventTypes)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required.", nameof(url));
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret is required.", nameof(secret));

        return new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Url = url,
            Secret = secret,
            SubscribedEventTypes = subscribedEventTypes.ToList(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsSubscribedTo(string eventType) => IsActive && SubscribedEventTypes.Contains(eventType);
}
