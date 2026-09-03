namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactActOccurrence
{
    public required Guid ActId { get; init; }

    public required string ActNumber { get; init; }

    public required string ActTitle { get; init; }

    public required DateOnly ActDate { get; init; }

    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    /// <summary>
    /// The reference number another authority gave the act, null where none is recorded (SDD-010).
    /// </summary>
    public string? ExternalNumber { get; init; }
}
