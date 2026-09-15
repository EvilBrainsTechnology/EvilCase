using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class ActEditModel : IValidatableObject
{
    public static ActEditModel From(ActDetail detail)
    {
        return new ActEditModel
        {
            ActNumber = detail.ActNumber,
            ExternalActNumber = detail.ExternalActNumber,
            Direction = detail.Direction,
            Date = detail.Date,
            Title = detail.Title,
            Description = detail.Description,
            Contact = detail.Contact,
            Labels = detail.Labels,
        };
    }

    /// <summary>
    /// Assigns another instance's values onto this one, so a page can refresh its bound model
    /// without replacing the instance its EditContext already tracks.
    /// </summary>
    public void CopyFrom(ActEditModel other)
    {
        this.ActNumber = other.ActNumber;
        this.ExternalActNumber = other.ExternalActNumber;
        this.Direction = other.Direction;
        this.Date = other.Date;
        this.Title = other.Title;
        this.Description = other.Description;
        this.Contact = other.Contact;
        this.Labels = other.Labels;
    }

    [Required(ErrorMessage = "Zadejte číslo jednací")]
    [StringLength(128, ErrorMessage = "Číslo jednací může mít nejvýše 128 znaků")]
    public string ActNumber { get; set; } = "";

    [StringLength(128, ErrorMessage = "Externí číslo jednací může mít nejvýše 128 znaků")]
    public string? ExternalActNumber { get; set; }

    public ActDirection? Direction { get; set; }

    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Zadejte název úkonu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis může mít nejvýše 4000 znaků")]
    public string? Description { get; set; }

    public ContactListItem? Contact { get; set; }

    public IReadOnlyList<LabelItem> Labels { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Direction is not null && this.Contact is null)
            yield return new ValidationResult("Vyberte kontakt, nebo zrušte výběr směru", [nameof(this.Contact)]);

        if (this.Contact is not null && this.Direction is null)
            yield return new ValidationResult("Vyberte směr úkonu, nebo zrušte výběr kontaktu", [nameof(this.Direction)]);
    }
}
