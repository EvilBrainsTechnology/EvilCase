using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class CaseTools
{
    [McpServerTool(Name = "list_cases", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Lists the cases (spisy), one page at a time, with the total count the filter leaves. take (1-100) is required, skip "
            + "starts at 0. status defaults to Open (every case not closed); scope defaults to RootOnly (cases without a parent), "
            + "parentCaseId lists the subcases of one case instead. search matches the case number and title; from and to bound "
            + "the case date; contactId names the contact; labelIds keeps cases carrying every label named.")]
    public static async Task<CaseListResponse> ListCases(ICaseReader cases, [Description("Paging, order and filters.")] CaseListRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        return await cases.ListCases(request, token);
    }

    [McpServerTool(Name = "count_cases_by_state", ReadOnly = true, OpenWorld = false)]
    [Description("Counts the cases in each state: active, waiting on an authority, closed.")]
    public static async Task<CaseStatusCounts> CountCasesByState(ICaseReader cases, CancellationToken token)
    {
        return await cases.CountCasesByStatus(token);
    }

    [McpServerTool(Name = "get_case", ReadOnly = true, OpenWorld = false)]
    [Description("Reads one case with its parent path, contact and labels.")]
    public static async Task<CaseDetail> GetCase(ICaseReader cases, [Description("The case.")] Guid caseId, CancellationToken token)
    {
        return await cases.GetCaseDetail(caseId, token) ?? throw McpProblem.Of(CaseProblems.NotFound);
    }

    [McpServerTool(Name = "create_case", Destructive = false, OpenWorld = false)]
    [Description(
        "Creates a case; its case number is issued from date. title is required (at most 256 characters), description at most "
            + "4000. parentCaseId makes it a subcase, contactId and labelIds must name existing ones. Returns the new case.")]
    public static async Task<CaseListItem> CreateCase(ICaseWriter writer, [Description("The new case.")] CreateCaseRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var result = await writer.CreateCase(request, token);

        return result.Outcome switch
        {
            CaseCreateOutcome.Created => result.Case!,
            CaseCreateOutcome.InvalidParent => throw McpProblem.Of(CaseProblems.InvalidParent, "The parent case does not exist."),
            CaseCreateOutcome.ContactNotFound => throw McpProblem.Of(ContactProblems.UnknownContact, "The contact named in the request does not exist."),
            CaseCreateOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "update_case", Destructive = false, OpenWorld = false)]
    [Description(
        "Replaces every field of a case; read it with get_case first. caseNumber must read EC/yyyyMMdd-nnn and stay unique; "
            + "title is required (at most 256 characters), description at most 4000, externalCaseNumber at most 128. labelIds is "
            + "the whole set the case carries afterwards. The parent must be neither the case nor one of its subcases.")]
    public static async Task<string> UpdateCase(
        ICaseWriter writer, [Description("The case.")] Guid caseId, [Description("The case's new fields.")] CaseEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.UpdateCase(caseId, request, token);

        return outcome switch
        {
            CaseUpdateOutcome.Updated => "The case was updated.",
            CaseUpdateOutcome.NotFound => throw McpProblem.Of(CaseProblems.NotFound),
            CaseUpdateOutcome.CaseNumberTaken => throw McpProblem.Of("Case number taken", "Another case already carries the number."),
            CaseUpdateOutcome.InvalidCaseNumber => throw McpProblem.Of("One or more validation errors occurred.", "CaseNumber: The case number must read EC/yyyyMMdd-nnn."),
            CaseUpdateOutcome.InvalidParent => throw McpProblem.Of(
                CaseProblems.InvalidParent, "The parent case must exist and must be neither the case itself nor one of its subordinates."),
            CaseUpdateOutcome.ContactNotFound => throw McpProblem.Of(ContactProblems.UnknownContact, "The contact named in the request does not exist."),
            CaseUpdateOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "delete_case", Destructive = true, OpenWorld = false)]
    [Description("Deletes a case with its subcases, acts, files and comments. Cannot be undone.")]
    public static async Task<string> DeleteCase(ICaseWriter writer, [Description("The case.")] Guid caseId, CancellationToken token)
    {
        var outcome = await writer.DeleteCase(caseId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => "The case was deleted.",
            DeleteOutcome.NotFound => throw McpProblem.Of(CaseProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "set_case_labels", Destructive = false, OpenWorld = false)]
    [Description("Sets the labels a case carries: labelIds is the whole set afterwards, a label left out is taken off.")]
    public static async Task<string> SetCaseLabels(
        ILabelAssignmentWriter writer, [Description("The case.")] Guid caseId, [Description("The labels.")] LabelAssignmentRequest request, CancellationToken token)
    {
        var outcome = await writer.SetCaseLabels(caseId, request, token);

        return outcome switch
        {
            LabelAssignmentOutcome.Assigned => "The labels of the case were set.",
            LabelAssignmentOutcome.OwnerNotFound => throw McpProblem.Of(CaseProblems.NotFound),
            LabelAssignmentOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }
}
