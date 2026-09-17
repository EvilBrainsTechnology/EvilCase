namespace EvilBrains.EvilCase.App.Components.Ec;

/// <summary>
/// One breadcrumb of EcPageHeader; the current one carries no link.
/// </summary>
public sealed record EcCrumb(string Text, string? Href);
