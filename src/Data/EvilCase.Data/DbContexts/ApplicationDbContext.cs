using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();

    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();

    public DbSet<Party> Parties => this.Set<Party>();

    public DbSet<Case> Cases => this.Set<Case>();

    public DbSet<CaseTag> CaseTags => this.Set<CaseTag>();

    public DbSet<CaseReference> CaseReferences => this.Set<CaseReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // As a name rather than as the enum's number: an operator reads the column, and renumbering the
        // enum must not silently promote every row.
        modelBuilder.Entity<User>()
            .Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<Party>()
            .Property(party => party.Kind)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<Case>()
            .Property(@case => @case.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        // A sub-case has no meaning without what it hangs under, so the sub-tree goes with its root.
        // Nothing deletes a case yet; the rule is here so that whatever does later cannot orphan one.
        modelBuilder.Entity<Case>()
            .HasMany(@case => @case.Children)
            .WithOne(@case => @case.Parent)
            .HasForeignKey(@case => @case.ParentCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A party accumulates history across cases, so it outlives any one mark that names it.
        modelBuilder.Entity<CaseReference>()
            .HasOne(reference => reference.AssignedBy)
            .WithMany()
            .HasForeignKey(reference => reference.AssignedByPartyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
