using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactReader(IDbSession dbSession, IUserContext userContext) : IContactReader
{
    public async Task<IReadOnlyList<ContactListItem>> ListContacts(ContactListRequest request, CancellationToken token)
    {
        return await dbSession.Current.Contacts
            .MatchingSearch(request.Search)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    // A detail is not a list query: the header, the default-contact flag and the three act sources
    // are separate reads, merged here.
    public async Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token)
    {
        var context = dbSession.Current;

        var contact = await context.Contacts.DetailOf(contactId, token);
        if (contact is null)
            return null;

        var isDefault = await context.Users
            .WithDefaultContact(contactId)
            .AnyAsync(token);

        var cases = await context.ExternalCaseNumbers
            .AssignedByContact(contactId)
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .ToListAsync(token);

        var issuedBy = await context.Acts
            .IssuedByContact(contactId)
            .AsActOccurrences(ContactActRole.IssuedBy)
            .ToListAsync(token);

        var addressedTo = await context.Acts
            .AddressedToContact(contactId)
            .AsActOccurrences(ContactActRole.AddressedTo)
            .ToListAsync(token);

        return contact with
        {
            IsDefault = isDefault,
            Cases = cases,
            Acts = ContactOccurrences.InDisplayOrder(issuedBy, addressedTo),
        };
    }

    public async Task<ContactListItem> GetDefaultContact(CancellationToken token)
    {
        return await dbSession.Current.Users.DefaultContactOf(userContext.UserId, token);
    }
}
