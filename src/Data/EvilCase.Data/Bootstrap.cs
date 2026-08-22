using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EvilBrains.EvilCase.Data;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseData(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.AddScoped<TimestampInterceptor>();
        serviceCollection.AddScoped<UserWriteInterceptor>();

        serviceCollection.AddLocalDbContext<ApplicationDbContext>();
        serviceCollection.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        serviceCollection.AddScoped<IDbSession, DbSession>();

        return serviceCollection;
    }

    public static IHealthChecksBuilder AddEvilCaseDataHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddDbContextCheck<ApplicationDbContext>("database", tags: tags);

        return builder;
    }

    /// <summary>
    /// Applies the migrations the database is missing. Awaited before the host starts serving, so a
    /// request never reaches a schema the build does not expect; a failure here stops the application.
    /// </summary>
    public static async Task MigrateEvilCaseDatabase(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        await migrator.Migrate(cancellationToken);
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

                options.UseNpgsql(connectionString, npgsql => npgsql.UseEvilCaseMigrations());
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.AddInterceptors(
                    serviceProvider.GetRequiredService<TimestampInterceptor>(),
                    serviceProvider.GetRequiredService<UserWriteInterceptor>());

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
