using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class CaseEditModel
{
    public static CaseEditModel From(CaseDetail detail)
    {
        return new CaseEditModel
        {
            ParentCase = detail.ParentCase,
            CaseNumber = detail.CaseNumber,
            ExternalCaseNumber = detail.ExternalCaseNumber,
            Date = detail.Date,
            Title = detail.Title,
            Description = detail.Description,
            Contact = detail.Contact,
            Labels = detail.Labels,
            Status = detail.Status,
        };
    }

    /// <summary>
    /// Assigns another instance's values onto this one, so a page can refresh its bound model
    /// without replacing the instance its EditContext already tracks.
    /// </summary>
    public void CopyFrom(CaseEditModel other)
    {
        this.ParentCase = other.ParentCase;
        this.CaseNumber = other.CaseNumber;
        this.ExternalCaseNumber = other.ExternalCaseNumber;
        this.Date = other.Date;
        this.Title = other.Title;
        this.Description = other.Description;
        this.Contact = other.Contact;
        this.Labels = other.Labels;
        this.Status = other.Status;
    }

    public CaseListItem? ParentCase { get; set; }

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

    public IReadOnlyList<LabelItem> Labels { get; set; } = [];

    public CaseStatus Status { get; set; } = CaseStatus.Active;
}
