using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EvilBrains.EvilCase.Data.Migrations.DbContexts;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(npgsql => npgsql.UseEvilCaseMigrations());

        var dbContext = new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
        return dbContext;
    }
}
