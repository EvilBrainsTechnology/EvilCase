using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Business.Seeding;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
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
        services.AddScoped<IActReader, ActReader>();
        services.AddScoped<IActWriter, ActWriter>();
        services.AddScoped<IExternalCaseNumberWriter, ExternalCaseNumberWriter>();
        services.AddScoped<IExternalActNumberWriter, ExternalActNumberWriter>();
        services.AddScoped<ICommentReader, CommentReader>();
        services.AddScoped<ICommentWriter, CommentWriter>();
        services.AddScoped<IContactReader, ContactReader>();
        services.AddScoped<IContactWriter, ContactWriter>();
        services.AddScoped<IFileReader, FileReader>();
        services.AddScoped<IFileWriter, FileWriter>();
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
    public static async Task SeedEvilCaseSampleData(this IHost host, CancellationToken token)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var dbSession = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SampleDataSeeder>>();

        // The sample-data seed runs before a tenant is known.
        var user = await dbSession.Current.Users
            .IgnoreQueryFilters()
            .OrderBy(user => user.Created)
            .ThenBy(user => user.Id)
            .FirstOrDefaultAsync(token);

        if (user is null)
        {
            logger.LogInformation("Sample data seed skipped, no user exists yet");
            return;
        }

        // The seed enters the tenant itself, so this guard names the tenant instead of leaning on the filter.
        var seeded = await dbSession.Current.Cases
            .IgnoreQueryFilters()
            .AnyAsync(@case => @case.TenantId == user.TenantId, token);

        if (seeded)
        {
            logger.LogInformation("Sample data seed skipped, tenant {TenantId} already holds a case", user.TenantId);
            return;
        }

        var seeder = scope.ServiceProvider.GetRequiredService<ISampleDataSeeder>();

        await seeder.SeedSampleData(user.TenantId, user.Id, token);
    }
}
