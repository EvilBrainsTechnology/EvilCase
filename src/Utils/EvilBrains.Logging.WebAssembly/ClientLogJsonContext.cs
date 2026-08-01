using System.Text.Json.Serialization;
using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// The unload payload is serialized without reflection, which the trimmer would break. The naming
/// policy matches the web defaults the generated API client uploads with, so both reach the same endpoint.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ClientLogBatch))]
internal sealed partial class ClientLogJsonContext : JsonSerializerContext;
