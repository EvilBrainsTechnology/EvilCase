using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Every add saves on its own, so the database stamps two rows with different <c>Created</c> values.
/// </summary>
internal sealed class TestTenant : IAsyncDisposable
{
    private readonly IDisposable entered;

    private readonly StubUserContext stubUserContext;

    private readonly Guid tenantId;

    private readonly Dictionary<string, int> sequences = [];

    private TestTenant(IDisposable entered, StubUserContext stubUserContext, ApplicationDbContext context, Guid tenantId, Guid userId)
    {
        this.entered = entered;
        this.stubUserContext = stubUserContext;
        this.Context = context;
        this.tenantId = tenantId;
        this.UserId = userId;
    }

    public ApplicationDbContext Context { get; }

    public Guid UserId { get; }

    public IUserContext UserContext => this.stubUserContext;

    public static async Task<TestTenant> Create(bool asHost = false)
    {
        var userContext = new StubUserContext();
        var seededTenantId = Guid.CreateVersion7();
        var seededUserId = Guid.CreateVersion7();
        var scope = userContext.Enter(seededTenantId, seededUserId);
        var context = asHost
            ? TestDatabase.CreateMigratedAsHost(userContext)
            : TestDatabase.CreateMigrated(userContext);

        var account = new Account { Name = "tests" };

        context.Accounts.Add(account);
        context.Tenants.Add(new Tenant { Id = seededTenantId, AccountId = account.Id, Name = "tenant" });
        context.Users.Add(new User
        {
            Id = seededUserId,
            TenantId = seededTenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
        });

        await context.SaveChangesAsync();

        return new TestTenant(scope, userContext, context, seededTenantId, seededUserId);
    }

    /// <summary>
    /// Sorted the way PostgreSQL sorts a uuid, so a test can write them out of that order.
    /// </summary>
    public static Guid[] SortedEntityIds(int count)
    {
        return
        [
            .. Enumerable.Range(0, count)
                .Select(static _ => Guid.CreateVersion7())
                .OrderBy(static entityId => entityId.ToString(), StringComparer.Ordinal),
        ];
    }

    public async Task<Contact> AddContact(
        string name,
        ContactKind kind = ContactKind.Authority,
        string? dataBoxId = null,
        string? address = null,
        Guid? contactId = null)
    {
        var contact = new Contact
        {
            Id = contactId ?? Guid.CreateVersion7(),
            TenantId = this.tenantId,
            Kind = kind,
            Name = name,
            DataBoxId = dataBoxId,
            Address = address,
        };

        return await this.Save(this.Context.Contacts, contact);
    }

    public async Task<Case> AddCase(
        DateOnly date,
        string title = "Případ",
        string? description = null,
        CaseStatus status = CaseStatus.Active,
        string? caseNumber = null,
        Guid? caseId = null,
        Guid? parentCaseId = null,
        string? externalCaseNumber = null,
        Contact? contact = null)
    {
        var @case = new Case
        {
            ParentCaseId = parentCaseId,
            Id = caseId ?? Guid.CreateVersion7(),
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseNumber = caseNumber ?? CaseNumberFormat.Compose(date, this.NextSequence(CaseNumberFormat.Prefix(date))),
            ExternalCaseNumber = externalCaseNumber,
            ContactId = contact?.Id,
            Date = date,
            Title = title,
            Description = description,
            Status = status,
        };

        return await this.Save(this.Context.Cases, @case);
    }

    /// <summary>
    /// A contact alone takes <see cref="ActDirection.Incoming"/>, the pair the check constraint requires.
    /// </summary>
    public async Task<Act> AddAct(
        Case @case,
        DateOnly date,
        string title = "Úkon",
        Contact? contact = null,
        string? actNumber = null,
        Guid? actId = null,
        ActDirection? direction = null,
        string? description = null,
        string? externalActNumber = null)
    {
        var prefix = ActNumberFormat.Prefix(@case.CaseNumber, date);

        var act = new Act
        {
            Id = actId ?? Guid.CreateVersion7(),
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseId = @case.Id,
            ActNumber = actNumber ?? ActNumberFormat.Compose(@case.CaseNumber, date, this.NextSequence(prefix)),
            ExternalActNumber = externalActNumber,
            Direction = contact is null ? null : direction ?? ActDirection.Incoming,
            ContactId = contact?.Id,
            Title = title,
            Date = date,
            Description = description,
        };

        return await this.Save(this.Context.Acts, act);
    }

    /// <summary>
    /// The e-mail is unique deployment-wide, hence the <see cref="Guid"/>.
    /// </summary>
    public async Task<User> AddUser()
    {
        var user = new User
        {
            TenantId = this.tenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
        };

        return await this.Save(this.Context.Users, user);
    }

    public async Task<Comment> AddCaseComment(Case @case, string body, Guid? authorId = null)
    {
        var comment = new Comment
        {
            TenantId = this.tenantId,
            UserId = authorId ?? this.UserId,
            CaseId = @case.Id,
            Body = body,
        };

        using var scope = this.stubUserContext.Enter(this.tenantId, comment.UserId);

        return await this.Save(this.Context.Comments, comment);
    }

    public async Task<Comment> AddActComment(Act act, string body, Guid? authorId = null)
    {
        var comment = new Comment
        {
            TenantId = this.tenantId,
            UserId = authorId ?? this.UserId,
            ActId = act.Id,
            Body = body,
        };

        using var scope = this.stubUserContext.Enter(this.tenantId, comment.UserId);

        return await this.Save(this.Context.Comments, comment);
    }

    public async Task<FileAsset> AddCaseFile(Case @case, string fileName = "dokument.pdf")
    {
        return await this.AddFile(@case.Id, actId: null, fileName);
    }

    public async Task<FileAsset> AddActFile(Act act, string fileName = "dokument.pdf")
    {
        return await this.AddFile(caseId: null, act.Id, fileName);
    }

    public async ValueTask DisposeAsync()
    {
        await this.Context.DisposeAsync();
        this.entered.Dispose();
    }

    private async Task<FileAsset> AddFile(Guid? caseId, Guid? actId, string fileName)
    {
        var fileAssetId = Guid.CreateVersion7();

        var file = new FileAsset
        {
            Id = fileAssetId,
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseId = caseId,
            ActId = actId,
            FileName = fileName,
            ContentHash = new string('a', 64),
            SizeBytes = 1,
            MediaType = "application/pdf",
            StoragePath = $"{this.tenantId}/{fileAssetId}",
        };

        return await this.Save(this.Context.FileAssets, file);
    }

    private int NextSequence(string prefix)
    {
        var next = this.sequences.TryGetValue(prefix, out var previous) ? previous + 1 : 1;
        this.sequences[prefix] = next;

        return next;
    }

    private async Task<TEntity> Save<TEntity>(DbSet<TEntity> set, TEntity entity)
        where TEntity : class
    {
        set.Add(entity);
        await this.Context.SaveChangesAsync();

        return entity;
    }
}
