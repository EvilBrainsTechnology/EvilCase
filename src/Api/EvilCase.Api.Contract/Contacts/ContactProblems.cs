namespace EvilBrains.EvilCase.Api.Contract.Contacts;

/// <summary>
/// Problem title an endpoint answers with when a contact id named in the request body does not exist.
/// </summary>
public static class ContactProblems
{
    public const string NotFound = "Contact not found";

    public const string UnknownContact = "Unknown contact";
}
