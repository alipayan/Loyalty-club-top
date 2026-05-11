using CustomerClub.BuildingBlocks.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerClub.BuildingBlocks.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddCustomerClubServiceDefaults(this IServiceCollection services, string serviceName)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        services.ConfigureHttpJsonOptions(options =>
        {`
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        services.AddHttpContextAccessor();
        services.AddSingleton(new ServiceIdentity(serviceName));

        return services;
    }

    public static WebApplication UseCustomerClubDefaultPipeline(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.Headers.ContainsKey(ObservabilityConventions.CorrelationHeader))
            {
                context.Request.Headers.Append(
                    ObservabilityConventions.CorrelationHeader,
                    context.TraceIdentifier);
            }

            await next();
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready");

        return app;
    }
}

public sealed record ServiceIdentity(string Name);
