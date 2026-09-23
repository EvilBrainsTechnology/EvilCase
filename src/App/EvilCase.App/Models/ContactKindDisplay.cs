using System.Diagnostics;
using EvilBrains.EvilCase.App.Components.Ec;
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
            _ => throw new UnreachableException(),
        };
    }

    public static EcBadgeTone Tone(ContactKind kind)
    {
        return kind switch
        {
            ContactKind.Authority => EcBadgeTone.Authority,
            ContactKind.Official => EcBadgeTone.Official,
            ContactKind.Person => EcBadgeTone.Person,
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// The suffix behind <c>ec-contact-monogram-{suffix}</c>.
    /// </summary>
    public static string Css(ContactKind kind)
    {
        return kind switch
        {
            ContactKind.Authority => "authority",
            ContactKind.Official => "official",
            ContactKind.Person => "person",
            _ => throw new UnreachableException(),
        };
    }
}
