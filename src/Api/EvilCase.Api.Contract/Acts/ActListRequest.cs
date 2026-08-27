using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListRequest
{
    /// <summary>
    /// The most rows a page returns; the whole list when absent.
    /// </summary>
    [Range(1, 100)]
    public int? Take { get; init; }
}
