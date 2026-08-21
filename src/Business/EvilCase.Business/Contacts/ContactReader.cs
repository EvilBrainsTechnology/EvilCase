using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactReader(IDbSession dbSession) : IContactReader
{
    public async Task<IReadOnlyList<ContactListItem>> List(ContactListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await dbSession.Current.Contacts
            .MatchingSearch(request.Search)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
    }
}
