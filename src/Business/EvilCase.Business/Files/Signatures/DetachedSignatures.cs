using System.Runtime.InteropServices;
using EvilBrains.EvilCase.Api.Contract.Files;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Pairs a detached signature (p7s, p7m) with the file of its name among the files of one case or act.
/// </summary>
internal static class DetachedSignatures
{
    /// <summary>
    /// The name a signature file signs: its name without the .p7s or .p7m extension; null for any other name.
    /// </summary>
    public static string? SignedFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        var isSignature = fileName.Length > extension.Length
            && (string.Equals(extension, ".p7s", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".p7m", StringComparison.OrdinalIgnoreCase));

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
    public static (FileListItem? SignedFile, DetachedSignatureProblem? Problem) Pair(Guid signatureFileId, string signatureFileName, IReadOnlyList<FileListItem> ownerFiles)
    {
        var signedFile = WithSignatureFiles(ownerFiles).SingleOrDefault(file => file.SignatureFileId == signatureFileId);
        if (signedFile is not null)
            return (signedFile, null);

        var signedName = SignedFileName(signatureFileName);

        return ownerFiles.Any(file => string.Equals(file.FileName, signedName, StringComparison.Ordinal))
            ? (null, DetachedSignatureProblem.Ambiguous)
            : (null, DetachedSignatureProblem.Missing);
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
            // One object is read and trailing bytes are left alone, as SignedCms leaves them.
            using var input = new Asn1InputStream(new MemoryStream(bytes.Array!, bytes.Offset, bytes.Count, writable: false), bytes.Count);
            if (input.ReadObject() is not { } read)
                return false;

            var contentInfo = ContentInfo.GetInstance(read);

            return contentInfo.ContentType.Equals(CmsObjectIdentifiers.SignedData)
                && SignedData.GetInstance(contentInfo.Content).EncapContentInfo.Content is null;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// The signed file id to its signature file id, for each signed name exactly one file carries and exactly one signature
    /// names.
    /// </summary>
    private static Dictionary<Guid, Guid> SignatureFiles(IReadOnlyList<FileListItem> files)
    {
        var byName = files.ToLookup(static file => file.FileName, StringComparer.Ordinal);

        return files
            .Where(static file => SignedFileName(file.FileName) is not null)
            .GroupBy(static file => SignedFileName(file.FileName)!, StringComparer.Ordinal)
            .Where(static group => group.Take(2).Count() == 1)
            .Where(group => byName[group.Key].Take(2).Count() == 1)
            .ToDictionary(group => byName[group.Key].Single().FileId, static group => group.Single().FileId);
    }
}
