using HarborShield.Application.Common.Interfaces;
using HarborShield.Domain.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Api.Middleware;

/// <summary>
/// If a POST to a vessel-position or cargo-manifest ingestion endpoint carries an
/// Idempotency-Key header, replays the original response for a repeated key instead of
/// re-processing (e.g. a client retrying after a timeout shouldn't create duplicate data).
/// Only successful (2xx) responses are cached - a failed attempt didn't create anything, so
/// there's nothing to protect against duplicating.
/// </summary>
public class IdempotencyMiddleware(RequestDelegate next)
{
    private const string HeaderName = "Idempotency-Key";

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        var isIngestionEndpoint = context.Request.Method == HttpMethods.Post &&
            (path.EndsWith("/positions", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith("/cargo-manifests", StringComparison.OrdinalIgnoreCase));

        if (!isIngestionEndpoint || !context.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            await next(context);
            return;
        }

        var key = keyValues.ToString();

        var existing = await db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key && r.Path == path, context.RequestAborted);

        if (existing is not null)
        {
            context.Response.StatusCode = existing.ResponseStatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next(context);

        buffer.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            db.IdempotencyRecords.Add(IdempotencyRecord.Create(key, path, context.Response.StatusCode, responseBody));
            await db.SaveChangesAsync(context.RequestAborted);
        }
    }
}
