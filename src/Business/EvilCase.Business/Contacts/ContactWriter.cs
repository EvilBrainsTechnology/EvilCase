using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession session, IUserContext userContext, TimeProvider timeProvider) : IContactWriter
{
    // The edit goes through ExecuteUpdate: the context reads NoTracking, so a change written onto the
    // entity would save nothing. The statement sets Updated itself (SDD-018).
    public async Task<ContactUpdateOutcome> Update(Guid id, ContactEditRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var rows = await session.Current.Contacts
            .WithId(id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(contact => contact.Name, normalized.Name)
                    .SetProperty(contact => contact.Kind, normalized.Kind)
                    .SetProperty(contact => contact.DataBoxId, normalized.DataBoxId)
                    .SetProperty(contact => contact.Address, normalized.Address)
                    .SetProperty(contact => contact.Updated, now),
                cancellationToken);

        return rows == 0 ? ContactUpdateOutcome.NotFound : ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var context = session.Current;

        var contact = await context.Contacts
            .WithId(id)
            .SingleOrDefaultAsync(cancellationToken);
        if (contact is null)
            return ContactDeleteOutcome.NotFound;

        var isDefault = await context.Users
            .WithDefaultContact(userContext, id)
            .AnyAsync(cancellationToken);
        if (isDefault)
            return ContactDeleteOutcome.DefaultContact;

        var referenced = await context.Contacts
            .WithId(id)
            .Referenced()
            .AnyAsync(cancellationToken);
        if (referenced)
            return ContactDeleteOutcome.Referenced;

        context.Contacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);

        return ContactDeleteOutcome.Deleted;
    }

    internal static ContactEditRequest Normalize(ContactEditRequest request)
    {
        return request with
        {
            Name = request.Name.Trim(),
            DataBoxId = Trimmed(request.DataBoxId),
            Address = Trimmed(request.Address),
        };
    }

    private static string? Trimmed(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
