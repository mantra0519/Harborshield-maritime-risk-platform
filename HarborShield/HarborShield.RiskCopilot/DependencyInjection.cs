using HarborShield.Application.RiskCases.Copilot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborShield.RiskCopilot;

public static class DependencyInjection
{
    public static IServiceCollection AddRiskCopilot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new ModelWeightsProvider(configuration));
        services.AddScoped<IEmbeddingService, LlamaEmbeddingService>();
        services.AddScoped<IRiskCaseExplainer, LlamaRiskCaseExplainer>();

        return services;
    }
}
