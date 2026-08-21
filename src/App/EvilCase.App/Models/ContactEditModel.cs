using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the contact edit form binds to. Separate from the request contract, whose messages are English.
/// </summary>
public sealed class ContactEditModel
{
    [Required(ErrorMessage = "Zadejte název.")]
    [MaxLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků.")]
    public string Name { get; set; } = "";

    public ContactKind Kind { get; set; } = ContactKind.Authority;

    [MaxLength(16, ErrorMessage = "ID datové schránky může mít nejvýše 16 znaků.")]
    public string? DataBoxId { get; set; }

    [MaxLength(1024, ErrorMessage = "Adresa může mít nejvýše 1024 znaků.")]
    public string? Address { get; set; }
}
