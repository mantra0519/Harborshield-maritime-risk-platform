using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HarborShield.Api.Authentication;
using HarborShield.Api.ErrorHandling;
using HarborShield.Api.Middleware;
using HarborShield.Application;
using HarborShield.Infrastructure;
using HarborShield.RiskCopilot;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;

namespace HarborShield.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}"));

        // Add services to the container.

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        // Minimal server-rendered dashboard (no auth, not rate-limited) so anyone with the
        // deployed URL can see the system working without needing an API key or a Postman
        // collection - the JSON API below remains the "real" auth-protected surface.
        builder.Services.AddRazorPages();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>());

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddRiskCopilot(builder.Configuration);

        builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned by caller IP - the natural upgrade path once customers have their own
            // API keys is to partition by key instead, so one customer's traffic can't affect
            // another's. Only applied to the real customer-facing controllers (via
            // [EnableRateLimiting("api")]) - not the fake-vendor/fake-customer test doubles,
            // which are called frequently by our own background workers.
            options.AddPolicy("api", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseExceptionHandler();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapControllers();
        app.MapRazorPages();

        app.Run();
    }
}
