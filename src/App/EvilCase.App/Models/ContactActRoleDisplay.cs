using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

public static class ContactActRoleDisplay
{
    public static string Text(ContactActRole role) => role switch
    {
        ContactActRole.IssuedBy => "Vydal",
        ContactActRole.AddressedTo => "Adresát",
        ContactActRole.NumberIssuer => "Přidělil číslo jednací",
        _ => "",
    };
}
