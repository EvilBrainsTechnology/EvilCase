using EvilBrains.AI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.AI.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TestItem> TestItems => this.Set<TestItem>();

    public DbSet<User> Users => this.Set<User>();
}
