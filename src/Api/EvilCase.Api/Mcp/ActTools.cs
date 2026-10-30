using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class ActTools
{
    [McpServerTool(Name = "list_acts", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Lists the acts (úkony), one page at a time, with the total count the filter leaves. take (1-100) is required, skip "
            + "starts at 0. caseId keeps the acts of one case; caseStatus narrows to acts of cases in that state (All by default); "
            + "search matches the act number and title; from and to bound the act date; contactId, direction and labelIds "
            + "(every label named) narrow further.")]
    public static async Task<ActListResponse> ListActs(IActReader acts, [Description("Paging, order and filters.")] ActListRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        return await acts.ListActs(request, token);
    }

    [McpServerTool(Name = "get_act", ReadOnly = true, OpenWorld = false)]
    [Description("Reads one act with its case, contact, labels and the timeline of its case.")]
    public static async Task<ActDetail> GetAct(IActReader acts, [Description("The act.")] Guid actId, CancellationToken token)
    {
        return await acts.GetActDetail(actId, token) ?? throw McpProblem.Of(ActProblems.ActNotFound);
    }

    [McpServerTool(Name = "create_act", Destructive = false, OpenWorld = false)]
    [Description(
        "Creates an act on a case; its act number is issued from date. title is required (at most 256 characters), description "
            + "at most 4000. direction and contactId come together or not at all. Returns the new act.")]
    public static async Task<ActListItem> CreateAct(IActWriter writer, [Description("The new act.")] CreateActRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var result = await writer.CreateAct(request, token);

        return result.Outcome switch
        {
            ActCreateOutcome.Created => result.Act!,
            ActCreateOutcome.CaseNotFound => throw McpProblem.Of(CaseProblems.NotFound),
            ActCreateOutcome.ContactNotFound => throw McpProblem.Of(ContactProblems.UnknownContact, "The contact named in the request does not exist."),
            ActCreateOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "update_act", Destructive = false, OpenWorld = false)]
    [Description(
        "Replaces every field of an act; read it with get_act first. actNumber must read <case-number>/yyyyMMdd-nnn and stay "
            + "unique; title is required (at most 256 characters), description at most 4000, externalActNumber at most 128. "
            + "direction and contactId come together or not at all. labelIds is the whole set the act carries afterwards.")]
    public static async Task<string> UpdateAct(
        IActWriter writer, [Description("The act.")] Guid actId, [Description("The act's new fields.")] ActEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.UpdateAct(actId, request, token);

        return outcome switch
        {
            ActUpdateOutcome.Updated => "The act was updated.",
            ActUpdateOutcome.NotFound => throw McpProblem.Of(ActProblems.ActNotFound),
            ActUpdateOutcome.ContactNotFound => throw McpProblem.Of(ContactProblems.UnknownContact, "The contact named in the request does not exist."),
            ActUpdateOutcome.ActNumberTaken => throw McpProblem.Of("Act number taken", "Another act already carries the number."),
            ActUpdateOutcome.InvalidActNumber => throw McpProblem.Of(
                "One or more validation errors occurred.", "ActNumber: The act number must read <case-number>/yyyyMMdd-nnn."),
            ActUpdateOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "delete_act", Destructive = true, OpenWorld = false)]
    [Description("Deletes an act with its files and comments. Cannot be undone.")]
    public static async Task<string> DeleteAct(IActWriter writer, [Description("The act.")] Guid actId, CancellationToken token)
    {
        var outcome = await writer.DeleteAct(actId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => "The act was deleted.",
            DeleteOutcome.NotFound => throw McpProblem.Of(ActProblems.ActNotFound),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "set_act_labels", Destructive = false, OpenWorld = false)]
    [Description("Sets the labels an act carries: labelIds is the whole set afterwards, a label left out is taken off.")]
    public static async Task<string> SetActLabels(
        ILabelAssignmentWriter writer, [Description("The act.")] Guid actId, [Description("The labels.")] LabelAssignmentRequest request, CancellationToken token)
    {
        var outcome = await writer.SetActLabels(actId, request, token);

        return outcome switch
        {
            LabelAssignmentOutcome.Assigned => "The labels of the act were set.",
            LabelAssignmentOutcome.OwnerNotFound => throw McpProblem.Of(ActProblems.ActNotFound),
            LabelAssignmentOutcome.LabelNotFound => throw McpProblem.Of(LabelProblems.UnknownLabel, "A label named in the request does not exist."),
            _ => throw new UnreachableException(),
        };
    }
}
