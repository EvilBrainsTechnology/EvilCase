using System.ComponentModel;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class SignatureTools
{
    [McpServerTool(Name = "read_file_signature", ReadOnly = true, OpenWorld = true)]
    [Description(
        "Verifies the ISDS seal of a data message (ZFO): state, certificate, timestamp and revocation. Asks the certificate "
            + "authority for the revocation status. Any other file reads as not a data message; one over 30 MB is not read.")]
    public static async Task<FileSignatureResponse> ReadFileSignature(
        IFileSignatureReader signatures,
        [Description("The file.")] Guid fileId,
        [Description("The version; the current one where omitted.")] int? version,
        CancellationToken token)
    {
        return await signatures.ReadFileSignature(fileId, version, token) ?? throw McpProblem.Of(FileProblems.NotFound);
    }

    [McpServerTool(Name = "list_file_signers", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Lists the signers of a signed file (p7m, p7s, at most 50, files up to 30 MB) without verifying them; a detached "
            + "signature also names the file it signs.")]
    public static async Task<FileSignersResponse> ListFileSigners(
        IFileSignerReader signers,
        [Description("The file.")] Guid fileId,
        [Description("The version; the current one where omitted.")] int? version,
        CancellationToken token)
    {
        return await signers.ListFileSigners(fileId, version, token) ?? throw McpProblem.Of(FileProblems.NotFound);
    }

    [McpServerTool(Name = "verify_file_signers", ReadOnly = true, OpenWorld = true)]
    [Description(
        "Verifies every signer of a signed file (p7m, p7s): signature, certificate chain, qualification and revocation, which "
            + "asks the certificate authorities. Same limits as list_file_signers.")]
    public static async Task<FileSignersResponse> VerifyFileSigners(
        IFileSignerVerifier verifier,
        [Description("The file.")] Guid fileId,
        [Description("The version; the current one where omitted.")] int? version,
        CancellationToken token)
    {
        return await verifier.VerifyFileSigners(fileId, version, token) ?? throw McpProblem.Of(FileProblems.NotFound);
    }
}
