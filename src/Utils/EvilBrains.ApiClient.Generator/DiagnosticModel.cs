namespace EvilBrains.ApiClient.Generator;

internal sealed record DiagnosticModel(string Id, LocationModel? Location, EquatableArray<string> Arguments);
