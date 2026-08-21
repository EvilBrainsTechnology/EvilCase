using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession session,
    ICaseNumberIssuer numbers,
    IUserContext userContext,
    ILogger<CaseWriter> logger) : ICaseWriter
{
    /// <summary>
    /// How many numbers one case may be issued. The generator reads the day's highest and the unique index
    /// settles the race, so the loser of one files again with the number the winner left free (SDD-008).
    /// </summary>
    private const int Attempts = 5;

    public async Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (var attempt = 1; ; attempt++)
        {
            var caseNumber = await numbers.NextCaseNumber(request.Date, cancellationToken);
            var @case = Build(request, caseNumber, userContext);

            session.Current.Cases.Add(@case);

            try
            {
                await session.Current.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                session.Current.Entry(@case).State = EntityState.Detached;

                logger.LogWarning("The case number {CaseNumber} was taken while the case was being filed", caseNumber);

                continue;
            }

            logger.LogInformation("Filed a new case. (caseId: {CaseId}, caseNumber: {CaseNumber})", @case.Id, @case.CaseNumber);

            return Describe(@case);
        }
    }

    /// <summary>
    /// Internal so a test builds the row without a database. A new case is Active and hangs under nothing.
    /// TenantId is left for the interceptor to stamp, as the sample seeder leaves it (SDD-018).
    /// </summary>
    internal static Case Build(CreateCaseRequest request, string caseNumber, IUserContext userContext) => new()
    {
        UserId = userContext.UserId,
        CaseNumber = caseNumber,
        Title = request.Title,
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
        Date = request.Date,
        Status = CaseStatus.Active,
    };

    private static CaseListItem Describe(Case @case) => new()
    {
        Id = @case.Id,
        CaseNumber = @case.CaseNumber,
        Title = @case.Title,
        Date = @case.Date,
        Status = @case.Status,
    };
}
