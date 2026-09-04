using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class CaseEditModel
{
    public Guid? ParentCaseId { get; set; }

    [Required(ErrorMessage = "Zadejte spisovou značku")]
    [StringLength(64, ErrorMessage = "Spisová značka může mít nejvýše 64 znaků")]
    public string CaseNumber { get; set; } = "";

    [StringLength(128, ErrorMessage = "Externí spisová značka může mít nejvýše 128 znaků")]
    public string? ExternalCaseNumber { get; set; }

    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Zadejte název spisu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis může mít nejvýše 4000 znaků")]
    public string? Description { get; set; }

    public ContactListItem? Contact { get; set; }

    public CaseStatus Status { get; set; } = CaseStatus.Active;
}
