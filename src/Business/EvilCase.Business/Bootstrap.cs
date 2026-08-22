using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Business.Seeding;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseBusiness(this IServiceCollection services)
    {
        services.AddEvilCaseData();

        services.AddScoped<ICaseReader, CaseReader>();
        services.AddScoped<ICaseWriter, CaseWriter>();
        services.AddScoped<IContactReader, ContactReader>();
        services.AddScoped<ISampleDataSeeder, SampleDataSeeder>();
        services.AddScoped<ICaseNumberIssuer, CaseNumberIssuer>();
        services.AddScoped<IActNumberIssuer, ActNumberIssuer>();

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseBusinessHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseDataHealthChecks(tags);

        return builder;
    }

    /// <summary>
    /// Fills a tenant that holds no case with the sample case tree (SDD-017). Runs after the administrator
    /// seed, which is what creates the tenant and the user it hangs on.
    /// </summary>
    public static async Task SeedEvilCaseSampleData(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var dbSession = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SampleDataSeeder>>();

        var user = await dbSession.Current.Users
            .OrderBy(user => user.Created)
            .ThenBy(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Sample data seed skipped, no user exists yet");
            return;
        }

        using var tenantScope = scope.ServiceProvider.GetRequiredService<ITenantContext>().Enter(user.TenantId);

        if (await dbSession.Current.Cases.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Sample data seed skipped, tenant {TenantId} already holds a case", user.TenantId);
            return;
        }

        await using var transaction = await dbSession.Current.Database.BeginTransactionAsync(cancellationToken);

        await scope.ServiceProvider.GetRequiredService<ISampleDataSeeder>().Seed(user.TenantId, user.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
