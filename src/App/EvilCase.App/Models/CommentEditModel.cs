using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.App.Models;

internal sealed class CommentEditModel
{
    [Required(ErrorMessage = "Zadejte text komentáře")]
    public string Body { get; set; } = "";
}
