using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class LabelEditModel
{
    [Required(ErrorMessage = "Zadejte název štítku")]
    [StringLength(64, ErrorMessage = "Název může mít nejvýše 64 znaků")]
    public string Name { get; set; } = "";

    public LabelColor Color { get; set; } = LabelColor.Blue;
}
