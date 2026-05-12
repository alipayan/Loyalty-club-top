using CustomerClub.BuildingBlocks.ServiceDefaults.Configuration;
using CustomerClub.BuildingBlocks.ServiceDefaults.Correlation;
using CustomerClub.BuildingBlocks.ServiceDefaults.Health;
using CustomerClub.BuildingBlocks.ServiceDefaults.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomerClub.BuildingBlocks.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddCustomerClubServiceDefaults(
        this IServiceCollection services,
        string serviceName)
    {
        return services.AddCustomerClubServiceDefaults(options =>
        {
            options.ServiceName = serviceName;
        });
    }

    public static IServiceCollection AddCustomerClubServiceDefaults(
        this IServiceCollection services,
        Action<ServiceDefaultsOptions> configureOptions)
    {
        var options = new ServiceDefaultsOptions();
        configureOptions(options);

        services.AddSingleton(options);
        services.AddSingleton(new ServiceIdentity(options.ServiceName));

        if (options.EnableHealthChecks)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Live]);
        }

        if (options.EnableJsonDefaults)
        {
            services.ConfigureHttpJsonOptions(jsonOptions =>
            {
                jsonOptions.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                jsonOptions.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        }

        if (options.EnableHttpContextAccessor)
        {
            services.AddHttpContextAccessor();
        }

        return services;
    }

    public static WebApplication UseCustomerClubDefaultPipeline(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<ServiceDefaultsOptions>();

        if (options.EnableCorrelation)
        {
            app.UseMiddleware<CorrelationMiddleware>();
        }

        if (options.EnableHealthChecks)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthCheckTags.Live)
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthCheckTags.Ready) || !check.Tags.Any()
            });
        }

        return app;
    }
}
