using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.App.Models;

public static class ContactKindDisplay
{
    public static string Text(ContactKind kind) => kind switch
    {
        ContactKind.Authority => "Úřad",
        ContactKind.Official => "Úřední osoba",
        ContactKind.Person => "Osoba",
        _ => "",
    };
}
