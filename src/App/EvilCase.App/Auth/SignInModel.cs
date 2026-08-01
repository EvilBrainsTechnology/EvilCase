using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// What the sign-in form binds to. Separate from the request contract, whose properties are init-only
/// and whose messages are English.
/// </summary>
internal sealed class SignInModel
{
    [Required(ErrorMessage = "Zadejte e-mail")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Zadejte heslo")]
    public string Password { get; set; } = "";
}
