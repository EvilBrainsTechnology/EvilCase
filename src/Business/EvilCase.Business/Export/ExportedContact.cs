using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// One contact's document in Kontakty.
/// </summary>
internal sealed record ExportedContact
{
    public string Type { get; } = "contact";

    public required Guid ContactId { get; init; }

    public required ContactKind Kind { get; init; }

    public required string Name { get; init; }

    public required string? DataBoxId { get; init; }

    public required string? Address { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime? Updated { get; init; }

    /// <summary>
    /// The contact's document in the archive.
    /// </summary>
    public string Path { get; init; } = "";
}
