using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

internal static class FakeCases
{
    public static CaseDetailResponse Detail(long id, string title) => new()
    {
        Id = id,
        CaseNumber = "EC-000",
        Title = title,
        Status = CaseStatus.Active,
        Tags = [],
        Created = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
    };

    public static CaseComment Comment(long id, string body) => new()
    {
        Id = id,
        Body = body,
        AuthorEmail = "user@evilcase.test",
        Created = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
    };
}
