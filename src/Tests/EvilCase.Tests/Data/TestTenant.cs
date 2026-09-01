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
/// A tenant of its own on the test database, with the account, the user and the user's default contact
/// a write needs. Two of them never see each other's rows, so a test that seeds one cleans up nothing.
/// Every add saves on its own, which is what makes the database stamp two rows with different
/// <c>Created</c> values.
/// </summary>
internal sealed class TestTenant : IAsyncDisposable
{
    private readonly IDisposable entered;

    private readonly StubUserContext stubUserContext;

    private readonly Guid tenantId;

    // The day's next sequence, so a seeded number is the one SDD-008 gives that day.
    private readonly Dictionary<string, int> sequences = [];

    private TestTenant(IDisposable entered, StubUserContext stubUserContext, ApplicationDbContext context, Guid tenantId, Guid userId, Contact defaultContact)
    {
        this.entered = entered;
        this.stubUserContext = stubUserContext;
        this.Context = context;
        this.tenantId = tenantId;
        this.UserId = userId;
        this.DefaultContact = defaultContact;
    }

    public ApplicationDbContext Context { get; }

    /// <summary>
    /// The seeded user's id, the one a write lands under absent another author.
    /// </summary>
    public Guid UserId { get; }

    public IUserContext UserContext => this.stubUserContext;

    /// <summary>
    /// The contact the seeded user prefills an act with. A delete aimed at it answers
    /// <c>DefaultContact</c>, so a test that needs a deletable contact adds one of its own.
    /// </summary>
    public Contact DefaultContact { get; }

    /// <summary>
    /// <paramref name="asHost"/> wires the context the way the host wires it, so a service under test
    /// writes rows the interceptor stamps with the tenant and the user.
    /// </summary>
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
        var defaultContact = new Contact { TenantId = seededTenantId, Kind = ContactKind.Person, Name = "default" };

        context.Accounts.Add(account);
        context.Tenants.Add(new Tenant { Id = seededTenantId, AccountId = account.Id, Name = "tenant" });
        context.Contacts.Add(defaultContact);
        context.Users.Add(new User
        {
            Id = seededUserId,
            TenantId = seededTenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = defaultContact.Id,
        });

        await context.SaveChangesAsync();

        return new TestTenant(scope, userContext, context, seededTenantId, seededUserId, defaultContact);
    }

    /// <summary>
    /// Identifiers in the order PostgreSQL sorts them, so a test can write them in another order and
    /// leave the write order and the identifier order disagreeing.
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

    /// <summary>
    /// A case whose own number is the one its date gives it, unless the caller writes the number itself.
    /// </summary>
    public async Task<Case> AddCase(
        DateOnly date,
        string title = "Případ",
        string? description = null,
        CaseStatus status = CaseStatus.Active,
        string? caseNumber = null,
        Guid? caseId = null,
        Guid? parentCaseId = null)
    {
        var @case = new Case
        {
            ParentCaseId = parentCaseId,
            Id = caseId ?? Guid.CreateVersion7(),
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseNumber = caseNumber ?? CaseNumberFormat.Compose(date, this.NextSequence(CaseNumberFormat.Prefix(date))),
            Date = date,
            Title = title,
            Description = description,
            Status = status,
        };

        return await this.Save(this.Context.Cases, @case);
    }

    public async Task<Act> AddAct(
        Case @case,
        DateOnly date,
        string title = "Úkon",
        Contact? issuedBy = null,
        Contact? addressedTo = null,
        string? actNumber = null,
        Guid? actId = null,
        ActDirection direction = ActDirection.Incoming,
        string? description = null)
    {
        var prefix = ActNumberFormat.Prefix(@case.CaseNumber, date);

        var act = new Act
        {
            Id = actId ?? Guid.CreateVersion7(),
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseId = @case.Id,
            ActNumber = actNumber ?? ActNumberFormat.Compose(@case.CaseNumber, date, this.NextSequence(prefix)),
            Direction = direction,
            Title = title,
            Date = date,
            Description = description,
            IssuedByContactId = (issuedBy ?? this.DefaultContact).Id,
            AddressedToContactId = addressedTo?.Id,
        };

        return await this.Save(this.Context.Acts, act);
    }

    /// <summary>
    /// A second user of the same tenant, for a test that needs another author. Its e-mail is unique
    /// deployment-wide, so two tests seeding one never collide.
    /// </summary>
    public async Task<User> AddUser()
    {
        var user = new User
        {
            TenantId = this.tenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = this.DefaultContact.Id,
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

    public async Task<ExternalCaseNumber> AddExternalCaseNumber(Case @case, string value, Contact assignedBy)
    {
        var number = new ExternalCaseNumber
        {
            TenantId = this.tenantId,
            UserId = this.UserId,
            CaseId = @case.Id,
            Value = value,
            AssignedByContactId = assignedBy.Id,
        };

        return await this.Save(this.Context.ExternalCaseNumbers, number);
    }

    public async Task<ExternalActNumber> AddExternalActNumber(Act act, string value, Contact assignedBy)
    {
        var number = new ExternalActNumber
        {
            TenantId = this.tenantId,
            UserId = this.UserId,
            ActId = act.Id,
            Value = value,
            AssignedByContactId = assignedBy.Id,
        };

        return await this.Save(this.Context.ExternalActNumbers, number);
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
