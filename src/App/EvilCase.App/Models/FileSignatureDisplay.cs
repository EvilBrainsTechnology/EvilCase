using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.App.Models;

public static class FileSignatureDisplay
{
    public static string Reason(FileSignatureProblem problem)
    {
        return problem switch
        {
            FileSignatureProblem.NotDataMessage => "Soubor není podepsaná datová zpráva ani doručenka.",
            FileSignatureProblem.TooLarge => "Soubor je větší než 30 MB, podpis se neověřuje.",
            FileSignatureProblem.ContentMismatch => "Obsah zprávy neodpovídá podpisu, zpráva byla po podpisu změněna.",
            FileSignatureProblem.NotPostSignum => "Certifikát podpisu nevydala certifikační autorita PostSignum.",
            FileSignatureProblem.NotIsdsSigner => "Zprávu nepodepsal Informační systém datových schránek.",
            FileSignatureProblem.NotValidAtTimestamp => "Certifikát podpisu nebyl platný v okamžiku časového razítka.",
            FileSignatureProblem.TimestampInvalid => "Časové razítko podpisu chybí nebo není platné.",
            FileSignatureProblem.Revoked => "Certifikát podpisu byl zneplatněn.",
            _ => throw new UnreachableException(),
        };
    }
}
