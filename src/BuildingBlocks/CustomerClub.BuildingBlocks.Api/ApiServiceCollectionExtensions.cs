using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerClub.BuildingBlocks.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerClubApiConventions(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = false;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}
