using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

internal static class ActContactRules
{
    public static ContactListItem? Prefilled(ActDirection? direction, ContactListItem? actContact, ContactListItem? caseContact)
    {
        return direction is not null && actContact is null ? caseContact : actContact;
    }

    public static ContactListItem? DifferingCaseContact(ContactListItem? actContact, ContactListItem? caseContact)
    {
        return actContact is not null && caseContact is not null && actContact.ContactId != caseContact.ContactId
            ? caseContact
            : null;
    }
}
