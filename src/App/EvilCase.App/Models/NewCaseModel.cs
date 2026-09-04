using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class NewCaseModel
{
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Zadejte název spisu")]
    [StringLength(256, ErrorMessage = "Název může mít nejvýše 256 znaků")]
    public string Title { get; set; } = "";

    [StringLength(4000, ErrorMessage = "Popis může mít nejvýše 4000 znaků")]
    public string? Description { get; set; }

    public ContactListItem? Contact { get; set; }
}
