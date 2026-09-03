using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the act forms and the act detail make of the contact the parent case names (SDD-010).
/// </summary>
internal static class ActContactRules
{
    /// <summary>
    /// The contact the act carries once a direction is picked: the case's, while the act names none.
    /// </summary>
    public static ContactListItem? Prefilled(ActDirection? direction, ContactListItem? actContact, ContactListItem? caseContact)
    {
        return direction is not null && actContact is null ? caseContact : actContact;
    }

    /// <summary>
    /// The case's contact where the act names another one — what the warning names; null where there is
    /// nothing to warn about.
    /// </summary>
    public static ContactListItem? DifferingCaseContact(ContactListItem? actContact, ContactListItem? caseContact)
    {
        return actContact is not null && caseContact is not null && actContact.ContactId != caseContact.ContactId
            ? caseContact
            : null;
    }
}
