using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();
}
