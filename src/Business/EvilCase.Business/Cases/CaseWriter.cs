using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession dbSession,
    ICaseNumberIssuer numbers,
    ILogger<CaseWriter> logger) : ICaseWriter
{
    /// <summary>
    /// How many numbers one case may be issued. The generator reads the day's highest and the unique index
    /// settles the race, so the loser of one files again with the number the winner left free (SDD-008).
    /// </summary>
    private const int Attempts = 5;

    public async Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var caseNumber = await numbers.NextCaseNumber(request.Date, cancellationToken);
            var @case = Build(request, caseNumber);

            dbSession.Current.Cases.Add(@case);

            try
            {
                await dbSession.Current.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                dbSession.Current.Entry(@case).State = EntityState.Detached;

                logger.LogWarning("The case number {CaseNumber} was taken while the case was being filed", caseNumber);

                continue;
            }

            logger.LogInformation("Case {CaseId} was filed under {CaseNumber}", @case.Id, @case.CaseNumber);

            return Describe(@case);
        }
    }

    /// <summary>
    /// <c>TenantId</c> and <c>UserId</c> are left unset here, the way the sample seeder leaves them
    /// (SDD-018): the write stamps both from <c>IUserContext</c>.
    /// </summary>
    internal static Case Build(CreateCaseRequest request, string caseNumber)
    {
        return new()
        {
            CaseNumber = caseNumber,
            Title = request.Title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            Date = request.Date,
            Status = CaseStatus.Active,
        };
    }

    private static CaseListItem Describe(Case @case)
    {
        return new()
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
        };
    }
}
