using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class ContactEditModel
{
    [Required(ErrorMessage = "Zadejte název kontaktu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Name { get; set; } = "";

    public ContactKind Kind { get; set; } = ContactKind.Authority;

    [StringLength(16, ErrorMessage = "ID datové schránky může mít nejvýše 16 znaků")]
    public string? DataBoxId { get; set; }

    [StringLength(1024, ErrorMessage = "Adresa může mít nejvýše 1024 znaků")]
    public string? Address { get; set; }
}
