namespace EvilBrains.EvilCase.Business.Acts;

public enum ActUpdateOutcome
{
    Updated = 0,

    NotFound = 1,

    InvalidActNumber = 2,

    ActNumberTaken = 3,

    ContactNotFound = 4,
}
