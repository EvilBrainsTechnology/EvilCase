namespace EvilBrains.EvilCase.Api.Contract.Labels;

public static class LabelProblems
{
    public const string NotFound = "Label not found";

    public const string NameTaken = "Label name taken";

    public const string NameTakenDetail = "Another label already carries the name.";

    public const string UnknownLabel = "Unknown label";

    public const string UnknownLabelDetail = "A label named in the request does not exist.";
}
