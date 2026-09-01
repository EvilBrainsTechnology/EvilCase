using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class ExternalCaseNumberQuery
{
    public static IQueryable<ExternalCaseNumber> OfCase(this IQueryable<ExternalCaseNumber> numbers, Guid caseId)
    {
        return numbers.Where(number => number.CaseId == caseId);
    }

    /// <summary>
    /// In the order the marks accrued; the UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<ExternalCaseNumber> InAssignmentOrder(this IQueryable<ExternalCaseNumber> numbers)
    {
        return numbers.OrderBy(static number => number.Created).ThenBy(static number => number.Id);
    }

    public static IQueryable<ExternalNumberItem> AsItems(this IQueryable<ExternalCaseNumber> numbers)
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
