namespace EvilBrains.EvilCase.Data.Files;

/// <summary>
/// Where the bytes of every file asset live. The root of {root}/{tenantId}/{fileAssetId}.
/// </summary>
public sealed record FileStorageSettings(string RootPath);
