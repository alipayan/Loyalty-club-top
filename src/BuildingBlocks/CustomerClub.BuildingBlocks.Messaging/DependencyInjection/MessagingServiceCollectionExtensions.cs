using CustomerClub.BuildingBlocks.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerClub.BuildingBlocks.Messaging.DependencyInjection;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerClubMessagingCore(
        this IServiceCollection services)
    {
        services.AddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();

        return services;
    }
}