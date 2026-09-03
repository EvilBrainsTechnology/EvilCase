using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes the two places a contact appears: the cases that name it and the acts that name it instead of
/// the contact their case names.
/// </summary>
internal static class ContactOccurrenceQuery
{
    public static IQueryable<Case> WithContact(this IQueryable<Case> cases, Guid contactId)
    {
        return cases.Where(@case => @case.ContactId == contactId);
    }

    /// <summary>
    /// An act whose contact is the case's own is already listed under that case, so only the differing
    /// ones come back (SDD-011).
    /// </summary>
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
