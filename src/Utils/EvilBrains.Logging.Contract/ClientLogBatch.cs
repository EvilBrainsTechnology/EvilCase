using System.ComponentModel.DataAnnotations;

namespace EvilBrains.Logging.Contract;

public record ClientLogBatch
{
    public const int MaxEntries = 100;

    [MaxLength(MaxEntries)]
    public required IReadOnlyList<ClientLogEntry> Entries { get; init; }
}
