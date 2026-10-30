using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class LabelTools
{
    [McpServerTool(Name = "list_labels", ReadOnly = true, OpenWorld = false)]
    [Description("Lists every label of the tenant with how many cases and acts carry it. set_case_labels and set_act_labels assign them.")]
    public static async Task<LabelListResponse> ListLabels(ILabelReader labels, CancellationToken token)
    {
        return new LabelListResponse { Items = await labels.ListLabels(token) };
    }

    [McpServerTool(Name = "create_label", Destructive = false, OpenWorld = false)]
    [Description("Creates a label. name is required, at most 64 characters, and unique in the tenant. Returns the new label.")]
    public static async Task<LabelItem> CreateLabel(ILabelWriter writer, [Description("The new label.")] LabelEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var result = await writer.CreateLabel(request, token);

        return result.Outcome switch
        {
            LabelCreateOutcome.Created => result.Label!,
            LabelCreateOutcome.NameTaken => throw McpProblem.Of(LabelProblems.NameTaken, "Another label already carries the name."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "update_label", Destructive = false, OpenWorld = false)]
    [Description("Renames or recolours a label. name is required, at most 64 characters, and unique in the tenant.")]
    public static async Task<string> UpdateLabel(
        ILabelWriter writer, [Description("The label.")] Guid labelId, [Description("The label's new fields.")] LabelEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.UpdateLabel(labelId, request, token);

        return outcome switch
        {
            LabelUpdateOutcome.Updated => "The label was updated.",
            LabelUpdateOutcome.NotFound => throw McpProblem.Of(LabelProblems.NotFound),
            LabelUpdateOutcome.NameTaken => throw McpProblem.Of(LabelProblems.NameTaken, "Another label already carries the name."),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "delete_label", Destructive = true, OpenWorld = false)]
    [Description("Deletes a label and takes it off every case and act that carries it. Cannot be undone.")]
    public static async Task<string> DeleteLabel(ILabelWriter writer, [Description("The label.")] Guid labelId, CancellationToken token)
    {
        var outcome = await writer.DeleteLabel(labelId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => "The label was deleted.",
            DeleteOutcome.NotFound => throw McpProblem.Of(LabelProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
