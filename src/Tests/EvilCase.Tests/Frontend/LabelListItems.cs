using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal static class LabelListItems
{
    public static LabelListItem Of(LabelItem label, int caseCount = 0, int actCount = 0)
    {
        return new LabelListItem
        {
            LabelId = label.LabelId,
            Name = label.Name,
            Color = label.Color,
            CaseCount = caseCount,
            ActCount = actCount,
        };
    }
}
