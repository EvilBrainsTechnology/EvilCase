using System.Runtime.InteropServices;
using EvilBrains.EvilCase.Api.Contract.Files;
using Org.BouncyCastle.Cms;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Pairs a detached signature (p7s) with the file of its name among the files of one case or act; a p7m carries its
/// document inside and pairs with none.
/// </summary>
internal static class DetachedSignatures
{
    /// <summary>
    /// The name a signature file signs: its name without the .p7s extension; null for any other name.
    /// </summary>
    public static string? SignedFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        var isSignature = fileName.Length > extension.Length && string.Equals(extension, ".p7s", StringComparison.OrdinalIgnoreCase);

        return isSignature ? fileName[..^extension.Length] : null;
    }

    /// <summary>
    /// The files in their order, each file a signature signs by name carrying that signature's id.
    /// </summary>
    public static List<FileListItem> WithSignatureFiles(IReadOnlyList<FileListItem> files)
    {
        var signatures = SignatureFiles(files);

        return
        [
            .. files.Select(file => signatures.TryGetValue(file.FileId, out var signatureFileId) ? file with { SignatureFileId = signatureFileId } : file),
        ];
    }

    /// <summary>
    /// The file the signature signs among the files of its case or act, or why there is none.
    /// </summary>
    public static (FileListItem? SignedFile, DetachedSignatureProblem? Problem) Pair(Guid signatureFileId, IReadOnlyList<FileListItem> ownerFiles)
    {
        var signedFile = WithSignatureFiles(ownerFiles).SingleOrDefault(file => file.SignatureFileId == signatureFileId);

        return signedFile is not null ? (signedFile, null) : (null, DetachedSignatureProblem.Missing);
    }

    /// <summary>
    /// Whether the SignedData carries no content: a detached signature.
    /// </summary>
    public static bool IsDetached(ReadOnlyMemory<byte> cms)
    {
        if (!MemoryMarshal.TryGetArray(cms, out var bytes))
            bytes = cms.ToArray();

        try
        {
            // The parser streams: it stops at the encapsulated content and ignores what follows.
            using var stream = new MemoryStream(bytes.Array!, bytes.Offset, bytes.Count, writable: false);

            return new CmsSignedDataParser(stream).GetSignedContent() is null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // BouncyCastle reports malformed input through assorted exception types.
            return false;
        }
    }

    /// <summary>
    /// The signed file id to its signature file id; file names are unique within one case or act.
    /// </summary>
    private static Dictionary<Guid, Guid> SignatureFiles(IReadOnlyList<FileListItem> files)
    {
        var byName = files.ToDictionary(static file => file.FileName, static file => file.FileId, StringComparer.Ordinal);

        var signatures = new Dictionary<Guid, Guid>();
        foreach (var file in files)
        {
            if (SignedFileName(file.FileName) is { } signedName && byName.TryGetValue(signedName, out var signedFileId))
                signatures.Add(signedFileId, file.FileId);
        }

        return signatures;
    }
}
