using HarborShield.Application;
using HarborShield.Application.RiskCases.Copilot;
using HarborShield.Infrastructure;
using HarborShield.Infrastructure.Persistence;
using HarborShield.Infrastructure.Persistence.Seed;
using HarborShield.RiskCopilot;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HarborShield.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddRiskCopilot(builder.Configuration);

        builder.Services.AddHostedService<AnomalyDetectionWorker>();
        builder.Services.AddHostedService<SanctionsScreeningWorker>();
        builder.Services.AddHostedService<WebhookDeliveryWorker>();
        builder.Services.AddHostedService<RiskCaseEmbeddingWorker>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HarborShieldDbContext>();
            await RestrictedZoneSeeder.SeedAsync(db);

            var explainer = scope.ServiceProvider.GetRequiredService<IRiskCaseExplainer>();
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await DemoDataSeeder.SeedAsync(db, explainer, seedLogger);

            // Worker never generates explanations again after seeding (only RiskCaseEmbeddingWorker
            // runs continuously, and it only needs the much smaller embedding model) - free the
            // generation model's memory instead of holding it for the rest of this process's life.
            var modelProvider = scope.ServiceProvider.GetRequiredService<ModelWeightsProvider>();
            await modelProvider.UnloadGenerationWeightsAsync();
        }

        await host.RunAsync();
    }
}
