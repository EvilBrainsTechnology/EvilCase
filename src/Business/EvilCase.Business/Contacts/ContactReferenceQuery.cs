using EvilBrains.EvilCase.Data.DbContexts;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactReferenceQuery
{
    /// <summary>
    /// Every row that names the contact, as one query. Spans four entity sets, so it takes the
    /// context rather than one <c>IQueryable</c>.
    /// </summary>
    public static IQueryable<Guid> ReferencingContact(this ApplicationDbContext context, Guid contactId)
    {
        return context.ExternalCaseNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id)
            .Concat(context.Acts.Where(act => act.IssuedByContactId == contactId).Select(act => act.Id))
            .Concat(context.Acts.Where(act => act.AddressedToContactId == contactId).Select(act => act.Id))
            .Concat(context.ExternalActNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id));
    }
}
