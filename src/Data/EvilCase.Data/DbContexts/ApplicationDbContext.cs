using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();

    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();

    public DbSet<Party> Parties => this.Set<Party>();

    public DbSet<Case> Cases => this.Set<Case>();

    public DbSet<CaseRelation> CaseRelations => this.Set<CaseRelation>();

    public DbSet<CaseTag> CaseTags => this.Set<CaseTag>();

    public DbSet<ExternalCaseNumber> ExternalCaseNumbers => this.Set<ExternalCaseNumber>();

    public DbSet<Act> Acts => this.Set<Act>();

    public DbSet<FileAsset> FileAssets => this.Set<FileAsset>();

    public DbSet<ActFileReference> ActFileReferences => this.Set<ActFileReference>();

    public DbSet<Comment> Comments => this.Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        ConfigureEnums(modelBuilder);
        ConfigureCases(modelBuilder);
        ConfigureActs(modelBuilder);
        ConfigureFiles(modelBuilder);
        ConfigureComments(modelBuilder);
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
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        // The pair is stored once, so the check is what keeps a second row for the other direction — and
        // a row relating a case to itself — out of the table.
        modelBuilder.Entity<CaseRelation>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_CaseRelations_Ordered",
                @"""CaseId"" < ""RelatedCaseId"""));

        // A relation has no meaning without either end, and it is all a delete takes: the case at the
        // other end is a case of its own and stays.
        modelBuilder.Entity<CaseRelation>()
            .HasOne<Case>()
            .WithMany()
            .HasForeignKey(relation => relation.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaseRelation>()
            .HasOne<Case>()
            .WithMany()
            .HasForeignKey(relation => relation.RelatedCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExternalCaseNumber>()
            .HasOne(number => number.Case)
            .WithMany(@case => @case.ExternalCaseNumbers)
            .HasForeignKey(number => number.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A party accumulates history across cases, so it outlives any one mark that names it.
        modelBuilder.Entity<ExternalCaseNumber>()
            .HasOne(number => number.AssignedBy)
            .WithMany(party => party.AssignedExternalCaseNumbers)
            .HasForeignKey(number => number.AssignedByPartyId)
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
        // The bytes belong to the act they were filed under, so they go with it.
        modelBuilder.Entity<FileAsset>()
            .HasOne(asset => asset.Act)
            .WithMany(act => act.Files)
            .HasForeignKey(asset => asset.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActFileReference>()
            .HasOne(reference => reference.Act)
            .WithMany(act => act.FileReferences)
            .HasForeignKey(reference => reference.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        // An asset another act still reaches cannot go, and it aborts the delete of the act that owns it.
        modelBuilder.Entity<ActFileReference>()
            .HasOne(reference => reference.FileAsset)
            .WithMany(asset => asset.References)
            .HasForeignKey(reference => reference.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureComments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Comments_OnACaseOrAnAct",
                @"(""CaseId"" IS NULL) <> (""ActId"" IS NULL)"));

        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.Case)
            .WithMany(@case => @case.Comments)
            .HasForeignKey(comment => comment.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.Act)
            .WithMany(act => act.Comments)
            .HasForeignKey(comment => comment.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user's notes go with the user, as their cases and parties already do.
        modelBuilder.Entity<Comment>()
            .HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
