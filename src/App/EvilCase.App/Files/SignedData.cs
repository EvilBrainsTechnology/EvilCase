using System.Formats.Asn1;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A CMS SignedData read for its content only; the signature is not verified. A detached signature carries no content.
/// </summary>
internal sealed record SignedData(byte[]? Content)
{
    private const string SignedDataOid = "1.2.840.113549.1.7.2";

    private static readonly Asn1Tag ExplicitZero = new(TagClass.ContextSpecific, 0, isConstructed: true);

    /// <summary>
    /// The SignedData, or <see langword="null"/> for bytes that are none.
    /// </summary>
    public static SignedData? ParseOrDefault(byte[] cms)
    {
        try
        {
            var contentInfo = new AsnReader(cms, AsnEncodingRules.BER).ReadSequence();
            if (!string.Equals(contentInfo.ReadObjectIdentifier(), SignedDataOid, StringComparison.Ordinal))
                return null;

            var signedData = contentInfo.ReadSequence(ExplicitZero).ReadSequence();
            signedData.ReadEncodedValue(); // version
            signedData.ReadEncodedValue(); // digestAlgorithms
            var encapsulated = signedData.ReadSequence();
            encapsulated.ReadObjectIdentifier();

            return new SignedData(encapsulated.HasData ? encapsulated.ReadSequence(ExplicitZero).ReadOctetString() : null);
        }
        catch (AsnContentException)
        {
            return null;
        }
    }
}
