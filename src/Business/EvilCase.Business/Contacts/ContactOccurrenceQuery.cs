using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes each of the two places a contact can be named, one query per place.
/// </summary>
internal static class ContactOccurrenceQuery
{
    public static IQueryable<Act> IssuedByContact(this IQueryable<Act> acts, Guid contactId)
    {
        return acts.Where(act => act.IssuedByContactId == contactId);
    }

    public static IQueryable<Act> AddressedToContact(this IQueryable<Act> acts, Guid contactId)
    {
        return acts.Where(act => act.AddressedToContactId == contactId);
    }

    public static IQueryable<ContactActOccurrence> AsActOccurrences(this IQueryable<Act> acts, ContactActRole role)
    {
        return acts.Select(act => new ContactActOccurrence
        {
            ActId = act.Id,
            ActNumber = act.ActNumber,
            ActTitle = act.Title,
            ActDate = act.Date,
            CaseId = act.CaseId,
            CaseNumber = act.Case!.CaseNumber,
            Role = role,
            ExternalNumber = act.ExternalNumber,
        });
    }
}
