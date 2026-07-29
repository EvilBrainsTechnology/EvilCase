namespace EvilBrains.ApiClient.Generator;

internal sealed record ParameterModel(
    string Name,
    string Type,
    ParameterKind Kind,
    string WireName,
    bool IsNullable,
    string? DefaultValue,
    EquatableArray<QueryPropertyModel> QueryProperties);
