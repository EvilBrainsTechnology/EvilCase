using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession session, IUserContext userContext, TimeProvider timeProvider) : IContactWriter
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

        if (await context.Users.WithDefaultContact(userContext, id).AnyAsync(cancellationToken))
            return ContactDeleteOutcome.DefaultContact;

        if (await context.ReferencingContact(id).AnyAsync(cancellationToken))
            return ContactDeleteOutcome.Referenced;

        context.Contacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);

        return ContactDeleteOutcome.Deleted;
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
