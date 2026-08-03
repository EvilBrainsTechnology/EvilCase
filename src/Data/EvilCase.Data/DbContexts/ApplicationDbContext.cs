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

    public DbSet<Act> Acts => this.Set<Act>();

    public DbSet<FileAsset> FileAssets => this.Set<FileAsset>();

    public DbSet<ActFileLink> ActFileLinks => this.Set<ActFileLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        ConfigureEnums(modelBuilder);
        ConfigureCases(modelBuilder);
        ConfigureActs(modelBuilder);
        ConfigureFiles(modelBuilder);
    }

    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
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

        modelBuilder.Entity<Act>()
            .Property(act => act.Direction)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<ActFileLink>()
            .Property(link => link.Role)
            .HasConversion<string>()
            .HasMaxLength(32);
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        // A sub-case has no meaning without what it hangs under, so the sub-tree goes with its root.
        // Nothing deletes a case yet; the rule is here so that whatever does later cannot orphan one.
        modelBuilder.Entity<Case>()
            .HasMany(@case => @case.Children)
            .WithOne(@case => @case.Parent)
            .HasForeignKey(@case => @case.ParentCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaseReference>()
            .HasOne(reference => reference.Case)
            .WithMany(@case => @case.References)
            .HasForeignKey(reference => reference.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A party accumulates history across cases, so it outlives any one mark that names it.
        modelBuilder.Entity<CaseReference>()
            .HasOne(reference => reference.AssignedBy)
            .WithMany(party => party.AssignedCaseReferences)
            .HasForeignKey(reference => reference.AssignedByPartyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureActs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Act>()
            .HasOne(act => act.Case)
            .WithMany(@case => @case.Acts)
            .HasForeignKey(act => act.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A party accumulates history across cases, so it outlives any one act naming it. Both ends are
        // configured explicitly because two foreign keys to the same table cannot be inferred.
        modelBuilder.Entity<Act>()
            .HasOne(act => act.IssuedBy)
            .WithMany(party => party.IssuedActs)
            .HasForeignKey(act => act.IssuedByPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Act>()
            .HasOne(act => act.AddressedTo)
            .WithMany(party => party.AddressedActs)
            .HasForeignKey(act => act.AddressedToPartyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActFileLink>()
            .HasOne(link => link.Act)
            .WithMany(act => act.Files)
            .HasForeignKey(link => link.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        // The asset is shared, so it outlives any one link.
        modelBuilder.Entity<ActFileLink>()
            .HasOne(link => link.FileAsset)
            .WithMany(asset => asset.Links)
            .HasForeignKey(link => link.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // A second cascade path into this table would take another act's link with it.
        modelBuilder.Entity<ActFileLink>()
            .HasOne(link => link.OriginatingAct)
            .WithMany(act => act.AttachmentsTakenFromIt)
            .HasForeignKey(link => link.OriginatingActId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
