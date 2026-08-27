namespace EvilBrains.EvilCase.Business.Entities;

/// <summary>
/// How a delete ended, where the row is either there or not. A delete with more to say keeps its own
/// enum: <see cref="Contacts.ContactDeleteOutcome"/> also answers that the contact is referenced.
/// </summary>
public enum DeleteOutcome
{
    Deleted = 0,

    NotFound = 1,
}
