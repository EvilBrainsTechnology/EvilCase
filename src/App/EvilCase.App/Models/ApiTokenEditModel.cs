using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.Domain.ApiTokens;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class ApiTokenEditModel
{
    [Required(ErrorMessage = "Zadejte název tokenu")]
    [StringLength(ApiTokenLimits.MaxNameLength, ErrorMessage = "Název může mít nejvýše 64 znaků.")]
    public string Name { get; set; } = "";

    public ApiTokenScope Scope { get; set; } = ApiTokenScope.ReadOnly;

    public int ExpiresInDays { get; set; } = ApiTokenLimits.DefaultLifetimeDays;
}
