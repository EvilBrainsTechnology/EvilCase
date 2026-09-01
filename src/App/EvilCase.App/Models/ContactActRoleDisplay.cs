using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

public static class ContactActRoleDisplay
{
    public static string Text(ContactActRole role)
    {
        return role switch
        {
            ContactActRole.IssuedBy => "Odesílatel",
            ContactActRole.AddressedTo => "Příjemce",
            ContactActRole.NumberIssuer => "Přidělil číslo jednací",
            _ => "",
        };
    }
}
