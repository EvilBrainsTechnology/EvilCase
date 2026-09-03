using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the act edit form binds to. Separate from the request contract, whose properties are init-only
/// and whose messages are English.
/// </summary>
internal sealed class ActEditModel
{
    [Required(ErrorMessage = "Zadejte číslo jednací")]
    [StringLength(128, ErrorMessage = "Číslo jednací může mít nejvýše 128 znaků")]
    public string ActNumber { get; set; } = "";

    [StringLength(128, ErrorMessage = "Externí číslo jednací může mít nejvýše 128 znaků")]
    public string? ExternalActNumber { get; set; }

    public ActDirection Direction { get; set; } = ActDirection.Incoming;

    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Zadejte název úkonu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis může mít nejvýše 4000 znaků")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vyberte odesílatele")]
    public ContactListItem? IssuedByContact { get; set; }

    public ContactListItem? AddressedToContact { get; set; }
}
