using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What a comment form binds to. Separate from the request contract, whose messages are English.
/// </summary>
internal sealed class CommentEditModel
{
    [Required(ErrorMessage = "Zadejte text komentáře")]
    public string Body { get; set; } = "";
}
