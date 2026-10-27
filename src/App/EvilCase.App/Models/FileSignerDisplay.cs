using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.App.Models;

public static class FileSignerDisplay
{
    public static string Kind(FileSignerKind kind)
    {
        return kind switch
        {
            FileSignerKind.QualifiedSignature => "kvalifikovaný elektronický podpis",
            FileSignerKind.QualifiedCertificateSignature => "zaručený elektronický podpis založený na kvalifikovaném certifikátu",
            FileSignerKind.QualifiedSeal => "kvalifikovaná elektronická pečeť",
            FileSignerKind.QualifiedCertificateSeal => "zaručená elektronická pečeť založená na kvalifikovaném certifikátu",
            FileSignerKind.Other => "elektronický podpis",
            _ => throw new UnreachableException(),
        };
    }

    public static string ShortKind(FileSignerKind kind)
    {
        return kind switch
        {
            FileSignerKind.QualifiedSignature => "kvalifikovaný podpis",
            FileSignerKind.QualifiedCertificateSignature => "podpis s kvalifikovaným certifikátem",
            FileSignerKind.QualifiedSeal => "kvalifikovaná pečeť",
            FileSignerKind.QualifiedCertificateSeal => "pečeť s kvalifikovaným certifikátem",
            FileSignerKind.Other => "elektronický podpis",
            _ => throw new UnreachableException(),
        };
    }

    public static string Name(FileSigner signer)
    {
        return signer.Name.Length == 0 ? "Neznámý podepisující" : signer.Name;
    }

    /// <summary>
    /// The kind, the issuer and the timestamp on one line; the time the signer states where no timestamp is.
    /// </summary>
    public static string Summary(FileSigner signer)
    {
        var parts = new List<string> { ShortKind(signer.Kind) };
        if (signer.CertificateIssuer is { } issuer)
            parts.Add(issuer);

        if (signer.Timestamped is { } timestamped)
            parts.Add($"časové razítko {MomentDisplay.Text(timestamped)}");
        else if (signer.SigningTime is { } signingTime)
            parts.Add($"bez časového razítka, uvedeno {MomentDisplay.Text(signingTime)}");
        else
            parts.Add("bez časového razítka");

        return string.Join(" · ", parts);
    }

    public static string Certificate(FileSigner signer)
    {
        return signer.CertificateIssuer is { } issuer
            ? $"{issuer}, platný {Day(signer.CertificateValidFrom)} – {Day(signer.CertificateValidTo)}"
            : "—";
    }

    private static string Day(DateTime? moment)
    {
        return moment is { } value ? DateDisplay.Text(DateOnly.FromDateTime(value.ToLocalTime())) : "—";
    }
}
