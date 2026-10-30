using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The text an editor saves, handed to the upload of a new version as the file picked for it.
/// </summary>
internal sealed class EditedFile(string name, byte[] content) : IBrowserFile
{
    public string Name => name;

    public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

    public long Size => content.Length;

    public string ContentType => "text/plain";

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return new MemoryStream(content, writable: false);
    }
}
