using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class ExternalCaseNumberWriter(IDbSession dbSession, ILogger<ExternalCaseNumberWriter> logger) : IExternalCaseNumberWriter
{
    public async Task<ExternalCaseNumberOutcome> AddExternalCaseNumber(Guid caseId, ExternalNumberRequest request, CancellationToken token)
    {
        var value = request.Value.Trim();

        var context = dbSession.Current;

        var caseExists = await context.Cases.Exists(caseId, token);
        if (!caseExists)
            return ExternalCaseNumberOutcome.CaseNotFound;

        var contactExists = await context.Contacts.WithId(request.AssignedByContactId).AnyAsync(token);
        if (!contactExists)
            return ExternalCaseNumberOutcome.UnknownContact;

        var number = new ExternalCaseNumber
        {
            CaseId = caseId,
            Value = value,
            AssignedByContactId = request.AssignedByContactId,
        };

        context.ExternalCaseNumbers.Add(number);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            context.Entry(number).State = EntityState.Detached;

            return ExternalCaseNumberOutcome.ValueTaken;
        }

        logger.LogInformation("External case number {ExternalCaseNumberId} was added to case {CaseId}", number.Id, caseId);

        return ExternalCaseNumberOutcome.Added;
    }

    public async Task<DeleteOutcome> DeleteExternalCaseNumber(Guid caseId, Guid numberId, CancellationToken token)
    {
        var rows = await dbSession.Current.ExternalCaseNumbers
            .OfCase(caseId)
            .WithId(numberId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return DeleteOutcome.NotFound;

        logger.LogInformation("External case number {ExternalCaseNumberId} was removed from case {CaseId}", numberId, caseId);

        return DeleteOutcome.Deleted;
    }
}
