using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class SignInModel
{
    [Required(ErrorMessage = "Zadejte e-mail")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Zadejte heslo")]
    public string Password { get; set; } = "";
}
