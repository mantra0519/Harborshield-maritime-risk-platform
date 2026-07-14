namespace HarborShield.Domain.Idempotency;

/// <summary>
/// Remembers the response for a given (Idempotency-Key, request path) pair so a retried
/// request (e.g. after a client timeout) replays the original response instead of
/// re-processing and creating duplicate data.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = default!;
    public string Path { get; private set; } = default!;
    public int ResponseStatusCode { get; private set; }
    public string ResponseBody { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord()
    {
    }

    public static IdempotencyRecord Create(string key, string path, int responseStatusCode, string responseBody)
    {
        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            Path = path,
            ResponseStatusCode = responseStatusCode,
            ResponseBody = responseBody,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
