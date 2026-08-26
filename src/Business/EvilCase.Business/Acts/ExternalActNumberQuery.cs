using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

internal static class ExternalActNumberQuery
{
    public static IQueryable<ExternalActNumber> OfAct(this IQueryable<ExternalActNumber> numbers, Guid actId)
    {
        return numbers.Where(number => number.ActId == actId);
    }

    /// <summary>
    /// In the order the numbers accrued; the UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<ExternalActNumber> InAssignmentOrder(this IQueryable<ExternalActNumber> numbers)
    {
        return numbers.OrderBy(number => number.Created).ThenBy(number => number.Id);
    }

    public static IQueryable<ExternalNumberItem> AsItems(this IQueryable<ExternalActNumber> numbers)
    {
        return numbers.Select(number => new ExternalNumberItem
        {
            NumberId = number.Id,
            Value = number.Value,
            AssignedByContactId = number.AssignedByContactId,
            AssignedByContactName = number.AssignedBy!.Name,
        });
    }
}
