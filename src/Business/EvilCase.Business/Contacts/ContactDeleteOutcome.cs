namespace EvilBrains.EvilCase.Business.Contacts;

public enum ContactDeleteOutcome
{
    Deleted = 0,

    NotFound = 1,

    Referenced = 2,
}
