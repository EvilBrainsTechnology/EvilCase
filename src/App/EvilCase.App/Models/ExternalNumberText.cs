namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// The Czech the external-number card reads, one instance per owner.
/// </summary>
public sealed record ExternalNumberText
{
    public required string Heading { get; init; }

    public required string EmptyTitle { get; init; }

    public required string ValueColumn { get; init; }

    public required string AddTakenError { get; init; }

    public required string AddOwnerGoneError { get; init; }

    public required string AddFailedError { get; init; }

    public required string DeleteTitle { get; init; }

    public required string DeleteQuestion { get; init; }

    public required string DeleteGoneError { get; init; }

    public required string DeleteFailedError { get; init; }

    public static readonly ExternalNumberText ForCase = new()
    {
        Heading = "Externí spisové značky",
        EmptyTitle = "Žádné externí značky",
        ValueColumn = "Značka",
        AddTakenError = "Tuto značku už spis nese.",
        AddOwnerGoneError = "Spis už neexistuje.",
        AddFailedError = "Značku se nepodařilo přidat. Zkuste to znovu.",
        DeleteTitle = "Smazat externí značku",
        DeleteQuestion = "Opravdu smazat značku",
        DeleteGoneError = "Značka už neexistuje.",
        DeleteFailedError = "Značku se nepodařilo smazat. Zkuste to za chvíli znovu.",
    };

    public static readonly ExternalNumberText ForAct = new()
    {
        Heading = "Externí čísla jednací",
        EmptyTitle = "Žádná externí čísla jednací",
        ValueColumn = "Číslo jednací",
        AddTakenError = "Toto číslo jednací už úkon nese.",
        AddOwnerGoneError = "Úkon už neexistuje.",
        AddFailedError = "Číslo jednací se nepodařilo přidat. Zkuste to znovu.",
        DeleteTitle = "Smazat externí číslo jednací",
        DeleteQuestion = "Opravdu smazat číslo jednací",
        DeleteGoneError = "Číslo jednací už neexistuje.",
        DeleteFailedError = "Číslo jednací se nepodařilo smazat. Zkuste to za chvíli znovu.",
    };
}
