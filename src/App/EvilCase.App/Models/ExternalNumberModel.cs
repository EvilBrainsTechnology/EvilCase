using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the add-a-number form binds to. Separate from the request contract, whose messages are English.
/// </summary>
internal sealed class ExternalNumberModel
{
    [Required(ErrorMessage = "Zadejte hodnotu")]
    [StringLength(128, ErrorMessage = "Hodnota může mít nejvýše 128 znaků")]
    public string Value { get; set; } = "";

    [Required(ErrorMessage = "Vyberte kontakt, který hodnotu přidělil")]
    public ContactListItem? AssignedBy { get; set; }
}
