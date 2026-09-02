using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ExternalActNumberWriter(IDbSession dbSession, ILogger<ExternalActNumberWriter> logger) : IExternalActNumberWriter
{
    public async Task<ExternalActNumberOutcome> AddExternalActNumber(Guid caseId, Guid actId, ExternalNumberRequest request, CancellationToken token)
    {
        var value = request.Value.Trim();

        var context = dbSession.Current;

        var actExists = await context.Acts.OfCase(caseId).Exists(actId, token);
        if (!actExists)
            return ExternalActNumberOutcome.ActNotFound;

        var contactExists = await context.Contacts.WithId(request.AssignedByContactId).AnyAsync(token);
        if (!contactExists)
            return ExternalActNumberOutcome.UnknownContact;

        var number = new ExternalActNumber
        {
            ActId = actId,
            Value = value,
            AssignedByContactId = request.AssignedByContactId,
        };

        context.ExternalActNumbers.Add(number);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            context.Entry(number).State = EntityState.Detached;

            return ExternalActNumberOutcome.ValueTaken;
        }

        logger.LogInformation("External act number {ExternalActNumberId} was added to act {ActId}", number.Id, actId);

        return ExternalActNumberOutcome.Added;
    }

    public async Task<DeleteOutcome> DeleteExternalActNumber(Guid caseId, Guid actId, Guid numberId, CancellationToken token)
    {
        var context = dbSession.Current;

        var rows = await context.ExternalActNumbers
            .OfAct(caseId, actId)
            .WithId(numberId)
            .ExecuteSoftDelete(token);

        if (rows == 0)
            return DeleteOutcome.NotFound;

        logger.LogInformation("External act number {ExternalActNumberId} was removed from act {ActId}", numberId, actId);

        return DeleteOutcome.Deleted;
    }
}
