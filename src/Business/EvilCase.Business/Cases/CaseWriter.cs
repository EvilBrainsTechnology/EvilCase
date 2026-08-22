using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession session,
    ICaseNumberIssuer numbers,
    ILogger<CaseWriter> logger) : ICaseWriter
{
    /// <summary>
    /// How many numbers one case may be issued. The generator reads the day's highest and the unique index
    /// settles the race, so the loser of one files again with the number the winner left free (SDD-008).
    /// </summary>
    private const int Attempts = 5;

    public async Task<CaseListItem> Create(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            var caseNumber = await numbers.NextCaseNumber(request.Date, cancellationToken);
            var @case = Build(request, caseNumber);

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

            logger.LogInformation("Case {CaseId} was filed under {CaseNumber}", @case.Id, @case.CaseNumber);

            return Describe(@case);
        }
    }

    public async Task<CaseUpdateStatus> Update(Guid id, UpdateCaseRequest request, CancellationToken cancellationToken = default)
    {
        var number = request.CaseNumber.Trim();
        if (CaseNumberFormat.ParseOrDefault(number) is null)
            return CaseUpdateStatus.InvalidNumber;

        var @case = await session.Current.Cases.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (@case is null)
            return CaseUpdateStatus.NotFound;

        if (await session.Current.Cases.WithNumberTakenFrom(number, id).AnyAsync(cancellationToken))
            return CaseUpdateStatus.NumberTaken;

        session.Current.Entry(@case).CurrentValues.SetValues(Apply(@case, request, number));

        await session.Current.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Case {CaseId} was edited", @case.Id);

        return CaseUpdateStatus.Updated;
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

    /// <summary>
    /// The editable fields, onto the row as it stands; the tenant and the owner are not the form's to change.
    /// </summary>
    internal static Case Apply(Case @case, UpdateCaseRequest request, string caseNumber)
    {
        return @case with
        {
            CaseNumber = caseNumber,
            Date = request.Date,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = request.Status,
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
