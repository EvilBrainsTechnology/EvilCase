using EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilCase.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TestItem> TestItems => this.Set<TestItem>();

    public DbSet<User> Users => this.Set<User>();
}
