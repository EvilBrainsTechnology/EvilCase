using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CaseFileTreeQuery
{
    /// <summary>
    /// The case with every case below it, their acts and the acts' files (SDD-009); null where the case is unknown. The
    /// sub-cases come in the order of a case's sub-case list, the acts in the order of its act list.
    /// </summary>
    public static async Task<CaseFileTree?> ReadCaseFileTree(this ApplicationDbContext context, Guid caseId, CancellationToken token)
    {
        var cases = await context.CaseTree(caseId)
            .InSortOrder(CaseSortKey.Date, ListSortDirection.Descending)
            .AsListItems()
            .ToListAsync(token);

        if (cases.TrueForAll(@case => @case.CaseId != caseId))
            return null;

        var acts = await context.Acts
            .OfCases(cases.ConvertAll(static @case => @case.CaseId))
            .InSortOrder(ActSortKey.Date, ListSortDirection.Ascending)
            .AsListItems()
            .ToListAsync(token);

        var files = await context.FileAssets
            .OfActs(acts.ConvertAll(static act => act.ActId))
            .InUploadOrder()
            .AsActFiles()
            .ToListAsync(token);

        var subCases = cases.ToLookup(static @case => @case.ParentCaseId);
        var caseActs = acts.ToLookup(static act => act.CaseId);
        var actFiles = files.ToLookup(static file => file.ActId, static file => file.File);

        // A parent cycle, which two concurrent edits can store, leads back only to the case asked for.
        CaseFileTree Node(CaseListItem @case)
        {
            var children = subCases[@case.CaseId]
                .Where(subCase => subCase.CaseId != caseId)
                .Select(Node)
                .ToList();

            var ownActs = caseActs[@case.CaseId]
                .Select(act => new ActFileTree
                {
                    Act = act,
                    Files = DetachedSignatures.WithSignatureFiles([.. actFiles[act.ActId]]),
                })
                .ToList();

            return new CaseFileTree
            {
                Case = @case with { HasFiles = children.Exists(static child => child.Case.HasFiles) || ownActs.Exists(static act => act.Files.Count > 0) },
                SubCases = children,
                Acts = ownActs,
            };
        }

        return Node(cases.Single(@case => @case.CaseId == caseId));
    }
}
