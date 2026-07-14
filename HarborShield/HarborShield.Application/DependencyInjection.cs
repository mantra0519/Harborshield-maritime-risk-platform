using System.Reflection;
using FluentValidation;
using HarborShield.Application.Common.Behaviors;
using HarborShield.Application.Vessels.AnomalyDetection;
using HarborShield.Application.Webhooks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace HarborShield.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediator(options =>
        {
            // Handlers depend on IApplicationDbContext, which is scoped - handlers must be too.
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton<VesselAnomalyDetector>();
        services.AddScoped<WebhookDeliveryEnqueuer>();

        return services;
    }
}
