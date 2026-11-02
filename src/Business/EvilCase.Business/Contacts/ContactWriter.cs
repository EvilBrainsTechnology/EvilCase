using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession dbSession, ILogger<ContactWriter> logger) : IContactWriter
{
    public async Task<ContactListItem> CreateContact(ContactEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var normalized = Normalize(request);

        var contact = new Contact
        {
            Kind = normalized.Kind,
            Name = normalized.Name,
            DataBoxId = normalized.DataBoxId,
            Address = normalized.Address,
        };

        context.Contacts.Add(contact);

        await context.SaveChangesAsync(token);

        logger.LogInformation("Contact {ContactId} was created", contact.Id);

        return new ContactListItem
        {
            ContactId = contact.Id,
            Kind = contact.Kind,
            Name = contact.Name,
            DataBoxId = contact.DataBoxId,
            Address = contact.Address,
        };
    }

    public async Task<ContactUpdateOutcome> UpdateContact(Guid contactId, ContactEditRequest request, CancellationToken token)
    {
        var normalized = Normalize(request);

        var rows = await dbSession.Current.Contacts
            .WithId(contactId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(static contact => contact.Name, normalized.Name)
                    .SetProperty(static contact => contact.Kind, normalized.Kind)
                    .SetProperty(static contact => contact.DataBoxId, normalized.DataBoxId)
                    .SetProperty(static contact => contact.Address, normalized.Address),
                token);

        if (rows == 0)
            return ContactUpdateOutcome.NotFound;

        logger.LogInformation("Contact {ContactId} was edited", contactId);

        return ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> DeleteContact(Guid contactId, CancellationToken token)
    {
        var contacts = dbSession.Current.Contacts
            .WithId(contactId);

        if (await contacts.AnyAsync(static contact => contact.Acts.Count != 0 || contact.Cases.Count != 0, token))
            return ContactDeleteOutcome.Referenced;

        var rows = await contacts.ExecuteDeleteAsync(token);

        if (rows == 0)
            return ContactDeleteOutcome.NotFound;

        logger.LogInformation("Contact {ContactId} was deleted", contactId);

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
