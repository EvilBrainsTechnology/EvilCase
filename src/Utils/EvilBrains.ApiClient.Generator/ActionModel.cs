namespace EvilBrains.ApiClient.Generator;

internal sealed record ActionModel(string Name, string HttpMethod, string Route, string? ResultType, bool ResultIsNullable, EquatableArray<ParameterModel> Parameters);
