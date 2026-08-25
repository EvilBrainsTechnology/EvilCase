using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CaseFileQuery
{
    /// <summary>
    /// The files a case delete takes: the case's own and those of its acts (SDD-007).
    /// </summary>
    public static IQueryable<FileAsset> OfCaseOrItsActs(this IQueryable<FileAsset> files, Guid caseId)
    {
        return files.Where(file => file.CaseId == caseId || file.Act!.CaseId == caseId);
    }
}
