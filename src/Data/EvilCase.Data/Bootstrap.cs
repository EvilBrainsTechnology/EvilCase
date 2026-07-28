using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EvilBrains.EvilCase.Data;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseData(this IServiceCollection serviceCollection)
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
                var connectionStringSection = configuration.GetRequiredSection("EvilBrains:EvilCase:ConnectionString");
                var connectionString = connectionStringSection.Value ?? throw new InvalidOperationException("Connection string not found");

                options.UseNpgsql(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

        return services;
    }
}
