namespace CustomerClub.ServiceTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceTemplateApplication(this IServiceCollection services)
    {
        services.AddScoped<SampleCommandHandler>();
        return services;
    }
}
