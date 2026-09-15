using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Labels;

internal sealed class LabelWriter(IDbSession dbSession, ILogger<LabelWriter> logger) : ILabelWriter
{
    public async Task<LabelCreateResult> CreateLabel(LabelEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var name = request.Name.Trim();

        if (await context.Labels.AnyAsync(label => label.Name == name, token))
            return new LabelCreateResult { Outcome = LabelCreateOutcome.NameTaken };

        var label = new Label { Name = name, Color = request.Color };

        context.Labels.Add(label);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // The name was taken between the check above and this save.
            return new LabelCreateResult { Outcome = LabelCreateOutcome.NameTaken };
        }

        logger.LogInformation("Label {LabelId} was created", label.Id);

        return new LabelCreateResult
        {
            Outcome = LabelCreateOutcome.Created,
            Label = new LabelItem { LabelId = label.Id, Name = label.Name, Color = label.Color },
        };
    }

    public async Task<LabelUpdateOutcome> UpdateLabel(Guid labelId, LabelEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var name = request.Name.Trim();

        if (!await context.Labels.Exists(labelId, token))
            return LabelUpdateOutcome.NotFound;

        if (await context.Labels.WithNameHeldByAnother(name, labelId).AnyAsync(token))
            return LabelUpdateOutcome.NameTaken;

        int rows;

        try
        {
            rows = await context.Labels
                .WithId(labelId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(static label => label.Name, name)
                        .SetProperty(static label => label.Color, request.Color),
                    token);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // The name was taken between the check above and this update.
            return LabelUpdateOutcome.NameTaken;
        }

        if (rows == 0)
            return LabelUpdateOutcome.NotFound;

        logger.LogInformation("Label {LabelId} was edited", labelId);

        return LabelUpdateOutcome.Updated;
    }

    public async Task<DeleteOutcome> DeleteLabel(Guid labelId, CancellationToken token)
    {
        // The assignments go with the row: the database's foreign keys carry the cascade (SDD-019).
        var rows = await dbSession.Current.Labels
            .WithId(labelId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return DeleteOutcome.NotFound;

        logger.LogInformation("Label {LabelId} was deleted", labelId);

        return DeleteOutcome.Deleted;
    }
}
