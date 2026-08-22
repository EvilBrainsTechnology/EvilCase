using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession session, TimeProvider timeProvider) : IContactWriter
{
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
                    .SetProperty(contact => contact.Address, normalized.Address),
                cancellationToken);

        return rows == 0 ? ContactUpdateOutcome.NotFound : ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var context = session.Current;

        var contact = await context.Contacts.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (contact is null)
            return ContactDeleteOutcome.NotFound;

        var isDefault = await context.Users
            .WithDefaultContact(id)
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
            DataBoxId = request.DataBoxId?.TrimEmptyToNull(),
            Address = request.Address?.TrimEmptyToNull(),
        };
    }
}
