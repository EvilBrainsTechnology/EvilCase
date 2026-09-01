using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

internal static class ExternalActNumberQuery
{
    public static IQueryable<ExternalActNumber> OfAct(this IQueryable<ExternalActNumber> numbers, Guid caseId, Guid actId)
    {
        return numbers
            .Where(number => number.ActId == actId)
            .Where(number => number.Act!.CaseId == caseId);
    }

    /// <summary>
    /// In the order the numbers accrued; the UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<ExternalActNumber> InAssignmentOrder(this IQueryable<ExternalActNumber> numbers)
    {
        return numbers.OrderBy(static number => number.Created).ThenBy(static number => number.Id);
    }

    public static IQueryable<ExternalNumberItem> AsItems(this IQueryable<ExternalActNumber> numbers)
    {
        return numbers.Select(static number => new ExternalNumberItem
        {
            ExternalNumberId = number.Id,
            Value = number.Value,
            AssignedByContactId = number.AssignedByContactId,
            AssignedByContactName = number.AssignedBy!.Name,
        });
    }
}
