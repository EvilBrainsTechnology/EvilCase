using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActWriter(IDbSession dbSession, IActNumberIssuer numbers, IFileBlobStore fileBlobStore, ILogger<ActWriter> logger) : IActWriter
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

        if (!await this.ContactsKnown(request.IssuedByContactId, request.AddressedToContactId, token))
            return new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound };

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
            Title = request.Title.Trim(),
            Date = request.Date,
            Description = request.Description?.TrimEmptyToNull(),
            IssuedByContactId = request.IssuedByContactId,
            AddressedToContactId = request.AddressedToContactId,
        };
    }

    public async Task<ActUpdateOutcome> UpdateAct(Guid caseId, Guid actId, ActEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        var acts = context.Acts
            .OfCase(caseId)
            .WithId(actId);

        // An act the tenant does not have is not found, whatever else the edit gets wrong.
        if (!await acts.AnyAsync(token))
            return ActUpdateOutcome.NotFound;

        var edit = request with
        {
            ActNumber = request.ActNumber.Trim(),
            ExternalActNumber = request.ExternalActNumber?.TrimEmptyToNull(),
            Title = request.Title.Trim(),
            Description = request.Description?.TrimEmptyToNull(),
        };

        if (ActNumberFormat.ParseOrDefault(edit.ActNumber) is null)
            return ActUpdateOutcome.InvalidActNumber;

        var taken = await context.Acts
            .WithNumberHeldByAnother(edit.ActNumber, actId)
            .AnyAsync(token);

        if (taken)
            return ActUpdateOutcome.ActNumberTaken;

        if (!await this.ContactsKnown(edit.IssuedByContactId, edit.AddressedToContactId, token))
            return ActUpdateOutcome.ContactNotFound;

        var rows = await acts.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(static act => act.ActNumber, edit.ActNumber)
                .SetProperty(static act => act.ExternalActNumber, edit.ExternalActNumber)
                .SetProperty(static act => act.Direction, edit.Direction)
                .SetProperty(static act => act.Date, edit.Date)
                .SetProperty(static act => act.Title, edit.Title)
                .SetProperty(static act => act.Description, edit.Description)
                .SetProperty(static act => act.IssuedByContactId, edit.IssuedByContactId)
                .SetProperty(static act => act.AddressedToContactId, edit.AddressedToContactId),
            token);

        if (rows == 0)
            return ActUpdateOutcome.NotFound;

        logger.LogInformation("Act {ActId} was edited", actId);

        return ActUpdateOutcome.Updated;
    }

    public async Task<DeleteOutcome> DeleteAct(Guid caseId, Guid actId, CancellationToken token)
    {
        var context = dbSession.Current;

        // Read before the delete: the rows are gone once it runs.
        var storagePaths = await context.FileAssets
            .Where(file => file.ActId == actId)
            .Select(static file => file.StoragePath)
            .ToListAsync(token);

        // The comments and files go with the row: the database's foreign keys carry the cascade (SDD-007).
        var rows = await context.Acts
            .OfCase(caseId)
            .WithId(actId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return DeleteOutcome.NotFound;

        // After the commit: a blob orphaned by a failed delete is tolerated, a lost one is not (SDD-012).
        foreach (var storagePath in storagePaths)
            await fileBlobStore.DeleteFileBlob(storagePath, token);

        logger.LogInformation("Act {ActId} was deleted", actId);

        return DeleteOutcome.Deleted;
    }

    /// <summary>
    /// The foreign key alone would take a contact of another tenant; the filtered read is what refuses it.
    /// </summary>
    private async Task<bool> ContactsKnown(Guid issuedByContactId, Guid? addressedToContactId, CancellationToken token)
    {
        var contacts = dbSession.Current.Contacts;

        if (!await contacts.WithId(issuedByContactId).AnyAsync(token))
            return false;

        return addressedToContactId is not { } contactId || await contacts.WithId(contactId).AnyAsync(token);
    }
}
