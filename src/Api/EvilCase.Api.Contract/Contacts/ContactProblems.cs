namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public static class ContactProblems
{
    public const string NotFound = "Contact not found";

    public const string UnknownContact = "Unknown contact";

    public const string UnknownContactDetail = "The contact named in the request does not exist.";

    public const string InUse = "Contact in use";

    public const string InUseDetail = "The contact is named by a case or an act.";
}
