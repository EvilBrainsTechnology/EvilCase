using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Logs;

public record ClientLogBatch
{
    public const int MaxEntries = 100;

    [MaxLength(MaxEntries)]
    public required IReadOnlyList<ClientLogEntry> Entries { get; init; }
}
