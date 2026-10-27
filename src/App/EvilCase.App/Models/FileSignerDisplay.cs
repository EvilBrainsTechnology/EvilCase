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

    public static string Problem(FileSignersProblem problem)
    {
        return problem switch
        {
            FileSignersProblem.NotSigned => "Podpisy souboru nelze přečíst.",
            FileSignersProblem.TooLarge => "Soubor je větší než 30 MB, podpisy se nečtou.",
            FileSignersProblem.TooManySigners => $"Soubor nese víc než {FileLimits.MaxSigners} podpisů, podpisy se nezobrazí.",
            _ => throw new UnreachableException(),
        };
    }

    public static string ProblemTitle(FileSignerProblem problem, string signatureFileName, FileSigner signer)
    {
        var signedName = FileTypeDisplay.SignedContentName(signatureFileName);

        return problem switch
        {
            FileSignerProblem.ContentMismatch => $"Soubor {signedName} neodpovídá podpisu.",
            FileSignerProblem.NotValidAtTimestamp => "Certifikát podpisu neplatil v okamžiku časového razítka.",
            FileSignerProblem.TimestampInvalid => "Časové razítko podpisu není platné.",
            FileSignerProblem.Revoked => "Certifikát podpisu byl odvolán.",
            FileSignerProblem.UntrustedIssuer => "Certifikát nevydala česká kvalifikovaná autorita.",
            FileSignerProblem.NotValidNow => "Certifikát podpisu dnes neplatí a podpis nemá časové razítko.",
            FileSignerProblem.CertificateMissing => "Soubor nenese certifikát podepisujícího.",
            FileSignerProblem.RevocationUnknown => $"Kontrola odvolání certifikátu u {signer.CertificateIssuer ?? "vydavatele"} není dostupná. Podpis i řetězec certifikátů jsou v pořádku.",
            FileSignerProblem.SignedFileMissing => "Podepsaný soubor nenalezen.",
            FileSignerProblem.SignedFileAmbiguous => "Podepsaný soubor nelze určit.",
            FileSignerProblem.SignedFileTooLarge => "Podepsaný soubor je větší než 30 MB, podpis se neověřuje.",
            FileSignerProblem.SigningCertificateMismatch => "Podpis neodkazuje na certifikát podepisujícího.",
            _ => throw new UnreachableException(),
        };
    }

    public static string? ProblemNote(FileSignerProblem problem, string signatureFileName)
    {
        var signedName = FileTypeDisplay.SignedContentName(signatureFileName);

        return problem switch
        {
            FileSignerProblem.UntrustedIssuer => "Obsah odpovídá podpisu. Kdo certifikát vydal, aplikace neověří.",
            FileSignerProblem.NotValidNow => "Bez časového razítka nelze určit, zda certifikát platil v okamžiku podpisu.",
            FileSignerProblem.SignedFileMissing => $"Samostatný podpis {signatureFileName} patří k souboru {signedName}, který mezi soubory není. Nahrajte ho pod tímto jménem.",
            FileSignerProblem.SignedFileAmbiguous => $"K souboru {signedName} patří víc samostatných podpisů, nebo je souborů tohoto jména víc. Ponechte jen jeden.",
            FileSignerProblem.ContentMismatch
                or FileSignerProblem.NotValidAtTimestamp
                or FileSignerProblem.TimestampInvalid
                or FileSignerProblem.Revoked
                or FileSignerProblem.CertificateMissing
                or FileSignerProblem.RevocationUnknown
                or FileSignerProblem.SignedFileTooLarge
                or FileSignerProblem.SigningCertificateMismatch => null,
            _ => throw new UnreachableException(),
        };
    }

    public static string SignedFileProblem(DetachedSignatureProblem problem, string signatureFileName)
    {
        var signedName = FileTypeDisplay.SignedContentName(signatureFileName);

        return problem switch
        {
            DetachedSignatureProblem.Missing => $"Soubor {signedName} mezi soubory není. Nahrajte ho pod tímto jménem.",
            DetachedSignatureProblem.Ambiguous => $"Soubor {signedName} nelze určit: souborů tohoto jména nebo samostatných podpisů k němu je víc.",
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// The issuer and the moment the certificate is judged valid at.
    /// </summary>
    public static string ValidCertificate(FileSigner signer)
    {
        return $"{signer.CertificateIssuer ?? "—"}, {(signer.Timestamped is null ? "platný ke dni ověření" : "platný k času razítka")}";
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
