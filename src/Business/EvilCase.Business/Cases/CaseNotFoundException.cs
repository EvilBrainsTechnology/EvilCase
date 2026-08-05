namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// No case of the caller's own carries the identifier asked for. Its own type so that an endpoint can
/// answer <c>404</c> for it while every other <see cref="InvalidOperationException"/> stays a
/// <c>500</c>.
/// </summary>
public sealed class CaseNotFoundException : InvalidOperationException
{
    public CaseNotFoundException()
    { }

    public CaseNotFoundException(string message)
        : base(message)
    { }

    public CaseNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    { }

    public static CaseNotFoundException For(long caseId) =>
        new(string.Create(CultureInfo.InvariantCulture, $"there is no case {caseId} of this owner's"));
}
