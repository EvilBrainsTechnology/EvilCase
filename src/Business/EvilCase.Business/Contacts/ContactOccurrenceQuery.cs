using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes each of the four places a contact can be named, one query per place.
/// </summary>
internal static class ContactOccurrenceQuery
{
    public static IQueryable<ExternalCaseNumber> AssignedByContact(this IQueryable<ExternalCaseNumber> numbers, Guid contactId)
    {
        return numbers.Where(number => number.AssignedByContactId == contactId);
    }

    public static IQueryable<ExternalCaseNumber> InCaseOccurrenceOrder(this IQueryable<ExternalCaseNumber> numbers)
    {
        return numbers
            .OrderByDescending(static number => number.Case!.Date)
            .ThenByDescending(static number => number.Case!.CaseNumber.Length)
            .ThenByDescending(static number => number.Case!.CaseNumber)
            .ThenBy(static number => number.Value);
    }

    public static IQueryable<ContactCaseOccurrence> AsCaseOccurrences(this IQueryable<ExternalCaseNumber> numbers)
    {
        return numbers.Select(static number => new ContactCaseOccurrence
        {
            CaseId = number.CaseId,
            CaseNumber = number.Case!.CaseNumber,
            CaseTitle = number.Case!.Title,
            CaseDate = number.Case!.Date,
            ExternalNumber = number.Value,
        });
    }

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
            ExternalNumber = null,
        });
    }
}
