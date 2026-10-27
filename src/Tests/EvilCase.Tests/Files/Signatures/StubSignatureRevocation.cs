using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// Answers every revocation check with one status and counts the checks. NSubstitute cannot proxy the internal interface.
/// </summary>
internal sealed class StubSignatureRevocation(SignatureRevocationStatus status) : ISignatureRevocation
{
    public static readonly DateTime CheckedAt = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    public int Checks { get; private set; }

    public SignatureRevocation CheckRevocation(X509Certificate2 certificate, SignatureTrust trust)
    {
        this.Checks++;

        return new SignatureRevocation { Status = status, Checked = CheckedAt };
    }
}
