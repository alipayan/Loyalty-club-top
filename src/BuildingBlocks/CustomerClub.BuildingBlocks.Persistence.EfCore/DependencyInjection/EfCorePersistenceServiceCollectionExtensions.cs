namespace CustomerClub.BuildingBlocks.Persistence.EfCore.DependencyInjection;

public static class EfCorePersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerClubEfCorePersistence<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IOutboxStore, EfCoreOutboxStore<TDbContext>>();
        services.AddScoped<IInboxStore, EfCoreInboxStore<TDbContext>>();

        return services;
    }

    public static IServiceCollection AddCustomerClubEfCoreOutbox<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IOutboxStore, EfCoreOutboxStore<TDbContext>>();

        return services;
    }

    public static IServiceCollection AddCustomerClubEfCoreInbox<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IInboxStore, EfCoreInboxStore<TDbContext>>();

        return services;
    }
}