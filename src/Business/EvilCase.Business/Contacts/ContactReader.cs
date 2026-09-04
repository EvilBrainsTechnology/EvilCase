using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactReader(IDbSession dbSession) : IContactReader
{
    public async Task<IReadOnlyList<ContactListItem>> ListContacts(ContactListRequest request, CancellationToken token)
    {
        return await dbSession.Current.Contacts
            .MatchingSearch(request.Search)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token)
    {
        var context = dbSession.Current;

        var contact = await context.Contacts.DetailOf(contactId, token);
        if (contact is null)
            return null;

        var cases = await context.Cases
            .WithContact(contactId)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);

        var acts = await context.Acts
            .WithContactDifferingFromItsCase(contactId)
            .InLatestOrder()
            .AsActOccurrences()
            .ToListAsync(token);

        return contact with { Cases = cases, Acts = acts };
    }
}
