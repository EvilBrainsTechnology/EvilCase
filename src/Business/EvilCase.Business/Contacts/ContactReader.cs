using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactReader(IDbSession dbSession) : IContactReader
{
    public async Task<ContactListResponse> ListContacts(ContactListRequest request, CancellationToken token)
    {
        var filtered = dbSession.Current.Contacts
            .MatchingSearch(request.Search)
            .WithKind(request.Kind);

        var total = await filtered.CountAsync(token);

        var items = await filtered
            .InSortOrder(request.Sort, request.SortDirection)
            .InPage(request)
            .AsListItems()
            .ToListAsync(token);

        return new ContactListResponse { Items = items, TotalCount = total };
    }

    public async Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token)
    {
        return await dbSession.Current.Contacts.DetailOf(contactId, token);
    }
}
