using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

public static class ContactKindDisplay
{
    public static string Text(ContactKind kind)
    {
        return kind switch
        {
            ContactKind.Authority => "Úřad",
            ContactKind.Official => "Úřední osoba",
            ContactKind.Person => "Člověk",
            _ => "",
        };
    }
}
