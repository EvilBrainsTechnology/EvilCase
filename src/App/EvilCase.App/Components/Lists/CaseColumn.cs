namespace EvilBrains.EvilCase.App.Components.Lists;

public enum CaseColumn
{
    Date = 0,

    /// <summary>
    /// The title with its labels and no parent line.
    /// </summary>
    Case = 1,

    /// <summary>
    /// The title with its labels and the parent case's title below it.
    /// </summary>
    CaseWithParent = 2,

    Status = 3,

    Changed = 4,

    /// <summary>
    /// The button opening the case's file tree, where the tree holds a file.
    /// </summary>
    Files = 5,
}
