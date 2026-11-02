namespace EvilBrains.EvilCase.Api.Contract.Cases;

public static class CaseProblems
{
    public const string NotFound = "Case not found";

    public const string InvalidParent = "Invalid parent";

    public const string ParentNotFoundDetail = "The parent case does not exist.";

    public const string InvalidParentDetail = "The parent case must exist and must be neither the case itself nor one of its subordinates.";

    public const string UnknownCase = "Unknown case";

    public const string UnknownCaseDetail = "The case named in the request does not exist.";

    public const string NumberTaken = "Case number taken";

    public const string NumberTakenDetail = "Another case already carries the number.";

    public const string InvalidNumberDetail = "The case number must read EC/yyyyMMdd-nnn.";
}
