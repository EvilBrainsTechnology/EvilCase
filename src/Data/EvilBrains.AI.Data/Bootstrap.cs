using EvilBrains.AI.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.AI.Data;

public static class Bootstrap
{
    public static IServiceCollection AddEvilBrainsAIData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLocalDbContext<ApplicationDbContext>();

        return serviceCollection;
    }

    private static IServiceCollection AddLocalDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(
            (serviceProvider, options) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStringSection = configuration.GetRequiredSection("EvilBrains:ConnectionString");
                var connectionString = connectionStringSection.Value ?? throw new InvalidOperationException("Connection string not found");

                options.UseNpgsql(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                // TODO add DB logging - options.AddDbLogging();
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

        return services;
    }
}
