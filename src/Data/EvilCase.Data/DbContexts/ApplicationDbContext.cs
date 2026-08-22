using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserContext userContext) : DbContext(options)
{
    private readonly IUserContext userContext = userContext;

    public DbSet<Account> Accounts => this.Set<Account>();

    public DbSet<Tenant> Tenants => this.Set<Tenant>();

    public DbSet<User> Users => this.Set<User>();

    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();

    public DbSet<Contact> Contacts => this.Set<Contact>();

    public DbSet<Case> Cases => this.Set<Case>();

    public DbSet<ExternalCaseNumber> ExternalCaseNumbers => this.Set<ExternalCaseNumber>();

    public DbSet<Act> Acts => this.Set<Act>();

    public DbSet<ExternalActNumber> ExternalActNumbers => this.Set<ExternalActNumber>();

    public DbSet<FileAsset> FileAssets => this.Set<FileAsset>();

    public DbSet<Comment> Comments => this.Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDbFunctions(modelBuilder);
        ConfigureExtensions(modelBuilder);
        ConfigureEntities(modelBuilder);
        this.ConfigureTenancy(modelBuilder);
        ConfigureAccounts(modelBuilder);
        ConfigureCases(modelBuilder);
        ConfigureActs(modelBuilder);
        ConfigureFiles(modelBuilder);
        ConfigureComments(modelBuilder);
        ConfigureEnums(modelBuilder);
    }

    private static void ConfigureDbFunctions(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDbFunction(typeof(DatabaseFunctions).GetMethod(nameof(DatabaseFunctions.Unaccent), [typeof(string)])!)
            .HasName("immutable_unaccent");
    }

    private static void ConfigureExtensions(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");
    }

    private static void ConfigureEntities(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(type => typeof(IEntity).IsAssignableFrom(type.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
            modelBuilder.Entity(entityType.ClrType).Property(nameof(IEntity.Id)).ValueGeneratedNever();
    }

    // Every enum is stored as its name, in a column as wide as the longest name that enum has.
    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
        var properties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(entityType => entityType.GetDeclaredProperties());

        foreach (var property in properties)
        {
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            if (!enumType.IsEnum)
                continue;

            property.SetProviderClrType(typeof(string));
            property.SetMaxLength(Enum.GetNames(enumType).Max(name => name.Length));
        }
    }

    private void ConfigureTenancy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>().HasQueryFilter(contact => contact.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Case>().HasQueryFilter(@case => @case.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<ExternalCaseNumber>().HasQueryFilter(number => number.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Act>().HasQueryFilter(act => act.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<ExternalActNumber>().HasQueryFilter(number => number.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<FileAsset>().HasQueryFilter(file => file.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Comment>().HasQueryFilter(comment => comment.TenantId == this.userContext.TenantIdOrDefault);

        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType))
            .ToList();

        foreach (var entityType in tenantEntityTypes)
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(Tenant))
                .WithMany()
                .HasForeignKey(nameof(ITenantEntity.TenantId))
                .OnDelete(DeleteBehavior.Restrict);
        }

        var userOwnedEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(type => typeof(IUserOwnedEntity).IsAssignableFrom(type.ClrType))
            .ToList();

        foreach (var entityType in userOwnedEntityTypes)
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(User))
                .WithMany()
                .HasForeignKey(nameof(IUserOwnedEntity.UserId))
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>()
            .HasOne(typeof(Account))
            .WithMany()
            .HasForeignKey(nameof(Tenant.AccountId))
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(typeof(Tenant))
            .WithMany()
            .HasForeignKey(nameof(User.TenantId))
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(user => user.DefaultContact)
            .WithMany()
            .HasForeignKey(user => user.DefaultContactId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>()
            .HasOne(@case => @case.ParentCase)
            .WithMany(@case => @case.ChildCases)
            .HasForeignKey(@case => @case.ParentCaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExternalCaseNumber>()
            .HasOne(number => number.Case)
            .WithMany(@case => @case.ExternalCaseNumbers)
            .HasForeignKey(number => number.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A contact accumulates history across cases, so it outlives any one mark that names it.
        modelBuilder.Entity<ExternalCaseNumber>()
            .HasOne(number => number.AssignedBy)
            .WithMany(contact => contact.AssignedExternalCaseNumbers)
            .HasForeignKey(number => number.AssignedByContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureActs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Act>()
            .HasOne(act => act.Case)
            .WithMany(@case => @case.Acts)
            .HasForeignKey(act => act.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A contact accumulates history across cases, so it outlives any one act naming it. Both ends
        // are configured explicitly because two foreign keys to the same table cannot be inferred.
        modelBuilder.Entity<Act>()
            .HasOne(act => act.IssuedByContact)
            .WithMany(contact => contact.IssuedActs)
            .HasForeignKey(act => act.IssuedByContactId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Act>()
            .HasOne(act => act.AddressedToContact)
            .WithMany(contact => contact.AddressedActs)
            .HasForeignKey(act => act.AddressedToContactId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExternalActNumber>()
            .HasOne(number => number.Act)
            .WithMany(act => act.ExternalActNumbers)
            .HasForeignKey(number => number.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExternalActNumber>()
            .HasOne(number => number.AssignedBy)
            .WithMany(contact => contact.AssignedExternalActNumbers)
            .HasForeignKey(number => number.AssignedByContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileAsset>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_FileAssets_OnACaseOrAnAct",
                @"(""CaseId"" IS NULL) <> (""ActId"" IS NULL)"));

        modelBuilder.Entity<FileAsset>()
            .HasOne(asset => asset.Case)
            .WithMany(@case => @case.Files)
            .HasForeignKey(asset => asset.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileAsset>()
            .HasOne(asset => asset.Act)
            .WithMany(act => act.Files)
            .HasForeignKey(asset => asset.ActId)
            .OnDelete(DeleteBehavior.Cascade);
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
    }
}
