using EvilBrains.AI.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EvilBrains.AI.Data.Migrations.DbContexts;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(
            x =>
            {
                x.MigrationsHistoryTable("_MigrationsHistory");
                x.MigrationsAssembly(this.GetType().Assembly.GetName().Name);
            });

        var dbContext = new ApplicationDbContext(optionsBuilder.Options);
        return dbContext;
    }
}
