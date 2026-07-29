namespace EvilBrains.ApiClient.Generator;

internal sealed record ApiModel(string Namespace, EquatableArray<ClientModel> Clients, EquatableArray<DiagnosticModel> Diagnostics);
