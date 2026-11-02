namespace EvilBrains.EvilCase.Api.Contract.Acts;

public static class ActProblems
{
    public const string NotFound = "Act not found";

    public const string NumberTaken = "Act number taken";

    public const string NumberTakenDetail = "Another act already carries the number.";

    public const string InvalidNumberDetail = "The act number must read <case-number>/yyyyMMdd-nnn.";
}
