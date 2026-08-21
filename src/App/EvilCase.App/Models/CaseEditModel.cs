using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the case edit form binds to. Separate from the request contract, whose properties are
/// init-only and whose messages are English.
/// </summary>
internal sealed class CaseEditModel
{
    [Required(ErrorMessage = "Zadejte spisovou značku")]
    [StringLength(64, ErrorMessage = "Spisová značka je nejvýše 64 znaků")]
    public string CaseNumber { get; set; } = "";

    [Required(ErrorMessage = "Zadejte datum spisu")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Zadejte název spisu")]
    [StringLength(256, ErrorMessage = "Název je nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis je nejvýše 4000 znaků")]
    public string? Description { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.Active;

    public static CaseEditModel From(CaseDetail @case)
    {
        ArgumentNullException.ThrowIfNull(@case);

        return new()
        {
            CaseNumber = @case.CaseNumber,
            Date = @case.Date,
            Title = @case.Title,
            Description = @case.Description,
            Status = @case.Status,
        };
    }

    public UpdateCaseRequest ToRequest() => new()
    {
        CaseNumber = this.CaseNumber,
        Date = this.Date!.Value,
        Title = this.Title,
        Description = this.Description,
        Status = this.Status,
    };
}
