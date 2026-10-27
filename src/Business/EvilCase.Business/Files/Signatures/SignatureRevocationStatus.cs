namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal enum SignatureRevocationStatus
{
    NotRevoked = 0,

    Revoked = 1,

    /// <summary>
    /// The issuer did not answer, or its answer did not settle the status.
    /// </summary>
    Unknown = 2,
}
