namespace EvilBrains.EvilCase.Business.Cases;

public enum CaseUpdateOutcome
{
    Updated = 0,

    NotFound = 1,

    InvalidCaseNumber = 2,

    CaseNumberTaken = 3,

    InvalidParent = 4,

    ContactNotFound = 5,
}
