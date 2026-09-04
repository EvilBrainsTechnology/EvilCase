using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

    public DbSet<Act> Acts => this.Set<Act>();

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
            .Where(static type => typeof(IEntity).IsAssignableFrom(type.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var entity = modelBuilder.Entity(entityType.ClrType);

            entity.Property(nameof(IEntity.Id)).ValueGeneratedNever();

            // A trigger stamps both, so a write sends neither and reads back what the database wrote.
            var created = entity.Property(nameof(IEntity.Created)).ValueGeneratedOnAdd().Metadata;
            created.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            created.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            var updated = entity.Property(nameof(IEntity.Updated)).ValueGeneratedOnAddOrUpdate().Metadata;
            updated.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            updated.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }

    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
        var properties = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(static entityType => entityType.GetDeclaredProperties());

        foreach (var property in properties)
        {
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            if (!enumType.IsEnum)
                continue;

            property.SetProviderClrType(typeof(string));
            property.SetMaxLength(Enum.GetNames(enumType).Max(static name => name.Length));
        }
    }

    private void ConfigureTenancy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasQueryFilter(user => user.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Contact>().HasQueryFilter(contact => contact.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Case>().HasQueryFilter(@case => @case.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Act>().HasQueryFilter(act => act.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<FileAsset>().HasQueryFilter(file => file.TenantId == this.userContext.TenantIdOrDefault);
        modelBuilder.Entity<Comment>().HasQueryFilter(comment => comment.TenantId == this.userContext.TenantIdOrDefault);

        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(static type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType))
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
            .Where(static type => typeof(IUserOwnedEntity).IsAssignableFrom(type.ClrType))
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

        modelBuilder.Entity<RefreshToken>()
            .HasOne(static token => token.User)
            .WithMany()
            .HasForeignKey(static token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>()
            .HasOne(static @case => @case.ParentCase)
            .WithMany(static @case => @case.ChildCases)
            .HasForeignKey(static @case => @case.ParentCaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Case>()
            .HasOne(static @case => @case.Contact)
            .WithMany(static contact => contact.Cases)
            .HasForeignKey(static @case => @case.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureActs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Act>()
            .HasOne(static act => act.Case)
            .WithMany(static @case => @case.Acts)
            .HasForeignKey(static act => act.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Act>()
            .ToTable(static table => table.HasCheckConstraint(
                "CK_Acts_DirectionWithContact",
                @"(""Direction"" IS NULL) = (""ContactId"" IS NULL)"));

        modelBuilder.Entity<Act>()
            .HasOne(static act => act.Contact)
            .WithMany(static contact => contact.Acts)
            .HasForeignKey(static act => act.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileAsset>()
            .ToTable(static table => table.HasCheckConstraint(
                "CK_FileAssets_OnACaseOrAnAct",
                @"(""CaseId"" IS NULL) <> (""ActId"" IS NULL)"));

        modelBuilder.Entity<FileAsset>()
            .HasOne(static asset => asset.Case)
            .WithMany(static @case => @case.Files)
            .HasForeignKey(static asset => asset.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileAsset>()
            .HasOne(static asset => asset.Act)
            .WithMany(static act => act.Files)
            .HasForeignKey(static asset => asset.ActId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureComments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>()
            .ToTable(static table => table.HasCheckConstraint(
                "CK_Comments_OnACaseOrAnAct",
                @"(""CaseId"" IS NULL) <> (""ActId"" IS NULL)"));

        modelBuilder.Entity<Comment>()
            .HasOne(static comment => comment.Case)
            .WithMany(static @case => @case.Comments)
            .HasForeignKey(static comment => comment.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(static comment => comment.Act)
            .WithMany(static act => act.Comments)
            .HasForeignKey(static comment => comment.ActId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
