using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Stands in for the reader wherever the test is about the HTTP surface rather than the database.
/// </summary>
internal sealed class RecordingCaseReader : ICaseReader
{
    public CaseListRequest? Request { get; private set; }

    public long? DetailId { get; private set; }

    public IReadOnlyList<CaseListItem> Items { get; init; } = [];

    public CaseDetailResponse? Detail { get; init; }

    Task<IReadOnlyList<CaseListItem>> ICaseReader.List(CaseListRequest request, CancellationToken cancellationToken)
    {
        this.Request = request;

        return Task.FromResult(this.Items);
    }

    Task<CaseDetailResponse?> ICaseReader.Detail(long id, CancellationToken cancellationToken)
    {
        this.DetailId = id;

        return Task.FromResult(this.Detail?.Id == id ? this.Detail : null);
    }
}
