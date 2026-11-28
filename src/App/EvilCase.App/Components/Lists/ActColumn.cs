namespace EvilBrains.EvilCase.App.Components.Lists;

public enum ActColumn
{
    Date = 0,

    ActWithCase = 1,

    /// <summary>
    /// The title with its labels and the act's contact below it, measured against the case's counterparty.
    /// </summary>
    ActWithContact = 2,

    /// <summary>
    /// The act's main file, opened in a preview from the row.
    /// </summary>
    MainFile = 3,

    Direction = 4,

    Status = 5,

    Contact = 6,

    Changed = 7,
}
