namespace EvilBrains.EvilCase.Data.Files;

/// <summary>
/// What a write measured: the checksum and the size the record stores.
/// </summary>
/// <param name="ContentHash">SHA-256 of the content, lower-case hex.</param>
/// <param name="SizeBytes">Size of the content in bytes.</param>
public sealed record FileBlobInfo(string ContentHash, long SizeBytes);
