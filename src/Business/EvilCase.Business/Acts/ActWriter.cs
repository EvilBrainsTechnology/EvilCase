using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActWriter(IDbSession dbSession, IActNumberIssuer numbers, ILogger<ActWriter> logger) : IActWriter
{
    /// <summary>
    /// How many numbers one act may be issued. The generator reads the day's highest and the unique index
    /// settles the race, so the loser of one files again with the number the winner left free (SDD-008).
    /// </summary>
    private const int Attempts = 5;

    public async Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        var @case = await context.Cases
            .WithId(caseId)
            .SingleOrDefaultAsync(token);

        if (@case is null)
            return new ActCreateResult { Outcome = ActCreateOutcome.CaseNotFound };

        for (var attempt = 1; ; attempt++)
        {
            var actNumber = await numbers.NextActNumber(@case, request.Date, token);
            var act = BuildAct(caseId, request, actNumber);

            context.Acts.Add(act);

            try
            {
                await context.SaveChangesAsync(token);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                context.Entry(act).State = EntityState.Detached;

                logger.LogWarning("The act number {ActNumber} was taken while the act was being filed", actNumber);

                continue;
            }
            catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
            {
                context.Entry(act).State = EntityState.Detached;

                // A sender or recipient the tenant does not hold; the foreign key is what says so.
                return new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound };
            }

            logger.LogInformation("Act {ActId} was filed in case {CaseId} under {ActNumber}", act.Id, caseId, act.ActNumber);

            var item = await context.Acts
                .WithId(act.Id)
                .AsListItems()
                .SingleAsync(token);

            return new ActCreateResult { Outcome = ActCreateOutcome.Created, Act = item };
        }
    }

    /// <summary>
    /// <c>TenantId</c> and <c>UserId</c> are left unset here, the way the sample seeder leaves them
    /// (SDD-018): the write stamps both from <c>IUserContext</c>.
    /// </summary>
    internal static Act BuildAct(Guid caseId, CreateActRequest request, string actNumber)
    {
        return new()
        {
            CaseId = caseId,
            ActNumber = actNumber,
            Direction = request.Direction,
            Title = request.Title,
            Date = request.Date,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            IssuedByContactId = request.IssuedByContactId,
            AddressedToContactId = request.AddressedToContactId,
        };
    }
}
