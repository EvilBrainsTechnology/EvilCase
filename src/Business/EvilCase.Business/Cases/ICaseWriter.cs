using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Writes the cases the user files.
/// </summary>
public interface ICaseWriter
{
    public Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken);
}
