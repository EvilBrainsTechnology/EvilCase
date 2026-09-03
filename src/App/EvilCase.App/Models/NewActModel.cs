using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the new-act form binds to. Separate from the request contract, whose properties are init-only
/// and whose messages are English.
/// </summary>
internal sealed class NewActModel : IValidatableObject
{
    public ActDirection? Direction { get; set; }

    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Zadejte název úkonu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis může mít nejvýše 4000 znaků")]
    public string? Description { get; set; }

    public ContactListItem? Contact { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Direction is not null && this.Contact is null)
            yield return new ValidationResult("Vyberte kontakt, nebo zrušte výběr směru", [nameof(this.Contact)]);

        if (this.Contact is not null && this.Direction is null)
            yield return new ValidationResult("Vyberte směr úkonu, nebo zrušte výběr kontaktu", [nameof(this.Direction)]);
    }
}
