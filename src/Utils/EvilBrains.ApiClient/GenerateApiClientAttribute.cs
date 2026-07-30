namespace EvilBrains.ApiClient;

/// <summary>
/// Marks an API controller for HTTP client generation by EvilBrains.ApiClient.Generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateApiClientAttribute : Attribute;
