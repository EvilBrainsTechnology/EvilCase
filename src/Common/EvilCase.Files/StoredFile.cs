namespace EvilBrains.EvilCase.Files;

/// <summary>
/// What a store learned about content while writing it.
/// </summary>
/// <param name="ContentHash">SHA-256 of the content, lower-case hex.</param>
/// <param name="SizeBytes">How long the content turned out to be.</param>
/// <param name="AlreadyPresent">
/// True when the same content was already stored, so nothing was written.
/// </param>
public readonly record struct StoredFile(string ContentHash, long SizeBytes, bool AlreadyPresent);
