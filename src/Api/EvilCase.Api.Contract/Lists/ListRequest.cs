using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

public abstract record ListRequest
{
    [Range(0, int.MaxValue)]
    public int Skip { get; init; }

    [Range(1, 100)]
    public required int Take { get; init; }
}
