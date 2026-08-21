using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Issues the numbers of SDD-008 and inserts the entity that carries one. Callers are business writers;
/// the API never sees an entity.
/// </summary>
public interface INumberIssuer
{
    /// <summary>
    /// Inserts the case the factory builds from the number issued for <paramref name="date"/>. The date is
    /// the case's own date; the caller passes the same value it puts on the entity.
    /// </summary>
    public Task<Case> InsertCase(DateOnly date, Func<string, Case> create, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the act the factory builds from the number issued under <paramref name="case"/> for
    /// <paramref name="date"/>.
    /// </summary>
    public Task<Act> InsertAct(Case @case, DateOnly date, Func<string, Act> create, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a hand-written case number is free, ignoring the case being edited.
    /// </summary>
    public Task<bool> IsCaseNumberFree(string number, Guid? excluding = null, CancellationToken cancellationToken = default);

    public Task<bool> IsActNumberFree(string number, Guid? excluding = null, CancellationToken cancellationToken = default);
}
