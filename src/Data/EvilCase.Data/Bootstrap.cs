using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EvilBrains.EvilCase.Data;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<UserWriteInterceptor>();

        serviceCollection.AddDbContext<ApplicationDbContext>(
            static (serviceProvider, options) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionStringSection = configuration.GetRequiredSection("EvilBrains:EvilCase:ConnectionString");
                var connectionString = connectionStringSection.Value ?? throw new InvalidOperationException("Connection string not found");

                options.UseNpgsql(connectionString, static npgsql => npgsql.UseEvilCaseMigrations());
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.AddInterceptors(serviceProvider.GetRequiredService<UserWriteInterceptor>());

                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

        serviceCollection.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        serviceCollection.AddScoped<IDbSession, DbSession>();

        return serviceCollection;
    }

    public static IHealthChecksBuilder AddEvilCaseDataHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddDbContextCheck<ApplicationDbContext>("database", tags: tags);

        return builder;
    }

    public static async Task MigrateEvilCaseDatabase(this IHost host, CancellationToken token)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        await migrator.Migrate(token);
    }
}
