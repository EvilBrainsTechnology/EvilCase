using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession session, TimeProvider timeProvider) : IContactWriter
{
    // The edit goes through ExecuteUpdate: the context reads NoTracking, so a change written onto the
    // entity would save nothing. The statement sets Updated itself (SDD-018).
    public async Task<ContactUpdateOutcome> Update(Guid id, ContactEditRequest request, CancellationToken cancellationToken = default)
    {
        var (name, kind, dataBoxId, address) = Normalized(request);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var rows = await session.Current.Contacts
            .Where(contact => contact.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(contact => contact.Name, name)
                    .SetProperty(contact => contact.Kind, kind)
                    .SetProperty(contact => contact.DataBoxId, dataBoxId)
                    .SetProperty(contact => contact.Address, address)
                    .SetProperty(contact => contact.Updated, now),
                cancellationToken);

        return rows == 0 ? ContactUpdateOutcome.NotFound : ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var context = session.Current;

        var contact = await context.Contacts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contact is null)
            return ContactDeleteOutcome.NotFound;

        if (await context.Users.AnyAsync(user => user.DefaultContactId == id, cancellationToken))
            return ContactDeleteOutcome.DefaultContact;

        if (await ReferencesTo(context, id).AnyAsync(cancellationToken))
            return ContactDeleteOutcome.Referenced;

        context.Contacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);

        return ContactDeleteOutcome.Deleted;
    }

    /// <summary>
    /// Every row that names the contact, as one query. Internal so a test reads the SQL the delete really runs.
    /// </summary>
    internal static IQueryable<Guid> ReferencesTo(ApplicationDbContext context, Guid contactId)
    {
        return context.ExternalCaseNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id)
            .Concat(context.Acts.Where(act => act.IssuedByContactId == contactId).Select(act => act.Id))
            .Concat(context.Acts.Where(act => act.AddressedToContactId == contactId).Select(act => act.Id))
            .Concat(context.ExternalActNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id));
    }

    /// <summary>
    /// What the edit writes. A field left blank is filed as nothing rather than as an empty string.
    /// </summary>
    internal static (string Name, ContactKind Kind, string? DataBoxId, string? Address) Normalized(ContactEditRequest request)
    {
        return (request.Name.Trim(), request.Kind, Trimmed(request.DataBoxId), Trimmed(request.Address));
    }

    private static string? Trimmed(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
