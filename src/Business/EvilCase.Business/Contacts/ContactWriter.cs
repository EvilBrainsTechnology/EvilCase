using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession dbSession) : IContactWriter
{
    public async Task<ContactUpdateOutcome> UpdateContact(Guid contactId, ContactEditRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);

        var rows = await dbSession.Current.Contacts
            .WithId(contactId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(contact => contact.Name, normalized.Name)
                    .SetProperty(contact => contact.Kind, normalized.Kind)
                    .SetProperty(contact => contact.DataBoxId, normalized.DataBoxId)
                    .SetProperty(contact => contact.Address, normalized.Address),
                cancellationToken);

        return rows == 0 ? ContactUpdateOutcome.NotFound : ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> DeleteContact(Guid contactId, CancellationToken cancellationToken)
    {
        var context = dbSession.Current;

        var contact = await context.Contacts.SingleOrDefaultAsync(entity => entity.Id == contactId, cancellationToken);
        if (contact is null)
            return ContactDeleteOutcome.NotFound;

        var isDefault = await context.Users
            .WithDefaultContact(contactId)
            .AnyAsync(cancellationToken);
        if (isDefault)
            return ContactDeleteOutcome.DefaultContact;

        var referenced = await context.Contacts
            .WithId(contactId)
            .Referenced()
            .AnyAsync(cancellationToken);
        if (referenced)
            return ContactDeleteOutcome.Referenced;

        context.Contacts.Remove(contact);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // A reference written between the checks above and this save.
            return ContactDeleteOutcome.Referenced;
        }

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
