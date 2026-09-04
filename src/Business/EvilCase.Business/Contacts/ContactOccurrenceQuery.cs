using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactOccurrenceQuery
{
    public static IQueryable<Case> WithContact(this IQueryable<Case> cases, Guid contactId)
    {
        return cases.Where(@case => @case.ContactId == contactId);
    }

    public static IQueryable<Act> WithContactDifferingFromItsCase(this IQueryable<Act> acts, Guid contactId)
    {
        return acts
            .Where(act => act.ContactId == contactId)
            .Where(act => act.Case!.ContactId != contactId);
    }

    public static IQueryable<ContactActOccurrence> AsActOccurrences(this IQueryable<Act> acts)
    {
        return acts.Select(static act => new ContactActOccurrence
        {
            ActId = act.Id,
            ActNumber = act.ActNumber,
            ActTitle = act.Title,
            ActDate = act.Date,
            CaseId = act.CaseId,
            CaseNumber = act.Case!.CaseNumber,
            ExternalNumber = act.ExternalActNumber,
        });
    }
}
