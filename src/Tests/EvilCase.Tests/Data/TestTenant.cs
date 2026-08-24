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

    // The day's next sequence, so a seeded number is the one SDD-008 gives that day.
    private readonly Dictionary<string, int> sequences = [];

    private TestTenant(IDisposable entered, ApplicationDbContext context, Guid tenantId, Guid userId, Contact defaultContact)
    {
        this.entered = entered;
        this.Context = context;
        this.TenantId = tenantId;
        this.UserId = userId;
        this.DefaultContact = defaultContact;
    }

    public ApplicationDbContext Context { get; }

    public Guid TenantId { get; }

    public Guid UserId { get; }

    /// <summary>
    /// The contact the seeded user prefills an act with. A delete aimed at it answers
    /// <c>DefaultContact</c>, so a test that needs a deletable contact adds one of its own.
    /// </summary>
    public Contact DefaultContact { get; }

    public static async Task<TestTenant> Create()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var scope = userContext.Enter(tenantId, userId);
        var context = TestDatabase.CreateMigrated(userContext);

        var account = new Account { Name = "tests" };
        var defaultContact = new Contact { TenantId = tenantId, Kind = ContactKind.Person, Name = "default" };

        context.Accounts.Add(account);
        context.Tenants.Add(new Tenant { Id = tenantId, AccountId = account.Id, Name = "tenant" });
        context.Contacts.Add(defaultContact);
        context.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = defaultContact.Id,
        });

        await context.SaveChangesAsync();

        return new TestTenant(scope, context, tenantId, userId, defaultContact);
    }

    public async Task<Contact> AddContact(
        string name,
        ContactKind kind = ContactKind.Authority,
        string? dataBoxId = null,
        string? address = null)
    {
        var contact = new Contact
        {
            TenantId = this.TenantId,
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
        string? caseNumber = null)
    {
        var @case = new Case
        {
            TenantId = this.TenantId,
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
        string? actNumber = null)
    {
        var prefix = ActNumberFormat.Prefix(@case.CaseNumber, date);

        var act = new Act
        {
            TenantId = this.TenantId,
            UserId = this.UserId,
            CaseId = @case.Id,
            ActNumber = actNumber ?? ActNumberFormat.Compose(@case.CaseNumber, date, this.NextSequence(prefix)),
            Direction = ActDirection.Incoming,
            Title = title,
            Date = date,
            IssuedByContactId = (issuedBy ?? this.DefaultContact).Id,
            AddressedToContactId = addressedTo?.Id,
        };

        return await this.Save(this.Context.Acts, act);
    }

    public async Task<ExternalCaseNumber> AddExternalCaseNumber(Case @case, string value, Contact assignedBy)
    {
        var number = new ExternalCaseNumber
        {
            TenantId = this.TenantId,
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
            TenantId = this.TenantId,
            UserId = this.UserId,
            ActId = act.Id,
            Value = value,
            AssignedByContactId = assignedBy.Id,
        };

        return await this.Save(this.Context.ExternalActNumbers, number);
    }

    public async ValueTask DisposeAsync()
    {
        await this.Context.DisposeAsync();
        this.entered.Dispose();
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
