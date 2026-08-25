using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the add-a-mark form binds to. Separate from the request contract, whose messages are English.
/// </summary>
internal sealed class ExternalCaseNumberModel
{
    [Required(ErrorMessage = "Zadejte značku")]
    [StringLength(128, ErrorMessage = "Značka může mít nejvýše 128 znaků")]
    public string Value { get; set; } = "";

    public ContactListItem? AssignedBy { get; set; }
}
