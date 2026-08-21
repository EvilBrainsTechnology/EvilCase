using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession session,
    ICaseNumberIssuer numbers,
    ITenantContext tenant,
    ILogger<CaseWriter> logger) : ICaseWriter
{
    public async Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caseNumber = await numbers.NextCaseNumber(request.Date, cancellationToken);
        var @case = Build(request, caseNumber, tenant);

        session.Current.Cases.Add(@case);
        await session.Current.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Filed a new case. (caseId: {CaseId}, caseNumber: {CaseNumber})", @case.Id, @case.CaseNumber);

        return new CaseListItem
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
        };
    }

    /// <summary>
    /// Internal so a test builds the row without a database. A new case is Active and hangs under nothing.
    /// TenantId is left for the interceptor to stamp, as the sample seeder leaves it (SDD-018).
    /// </summary>
    internal static Case Build(CreateCaseRequest request, string caseNumber, ITenantContext tenant) => new()
    {
        UserId = tenant.UserId,
        CaseNumber = caseNumber,
        Title = request.Title,
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
        Date = request.Date,
        Status = CaseStatus.Active,
    };
}
