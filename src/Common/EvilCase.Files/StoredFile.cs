namespace EvilBrains.EvilCase.Files;

/// <summary>
/// What a store learned about content while writing it. Both values are read from the bytes as they
/// went past, so neither can disagree with what is now on disk.
/// </summary>
/// <param name="ContentHash">SHA-256 of the content, lower-case hex.</param>
/// <param name="SizeBytes">How long the content turned out to be.</param>
/// <param name="AlreadyPresent">
/// True when the same content was already stored. The caller wanted the bytes kept, and they are —
/// this only says nothing was written, which is what makes an import safe to run twice.
/// </param>
public readonly record struct StoredFile(string ContentHash, long SizeBytes, bool AlreadyPresent);
