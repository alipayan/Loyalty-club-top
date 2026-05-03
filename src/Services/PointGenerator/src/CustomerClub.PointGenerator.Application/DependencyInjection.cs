using Microsoft.Extensions.DependencyInjection;

namespace CustomerClub.PointGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPointGeneratorApplication(this IServiceCollection services)
    {
        services.AddScoped<SampleCommandHandler>();
        return services;
    }
}
