using Serilog.Context;

namespace HarborShield.Api.Middleware;

/// <summary>
/// Reads (or generates) a correlation id for every request, echoes it back in the response
/// header, and pushes it into every log line written while handling that request - so one
/// customer-reported issue can be traced across all the logs it touched.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
