namespace EvilBrains.EvilCase.App.Models;

public static class FileSizeDisplay
{
    private const long Kilobyte = 1024;

    private const long Megabyte = 1024 * Kilobyte;

    private static readonly NumberFormatInfo CzechNumbers = NumberFormatInfo.ReadOnly(new NumberFormatInfo { NumberDecimalSeparator = "," });

    public static string Text(in long sizeBytes)
    {
        if (sizeBytes < Kilobyte)
            return string.Create(CzechNumbers, $"{sizeBytes}\u00A0B");

        if (sizeBytes < Megabyte)
            return string.Create(CzechNumbers, $"{sizeBytes / (double)Kilobyte:0.#}\u00A0kB");

        return string.Create(CzechNumbers, $"{sizeBytes / (double)Megabyte:0.#}\u00A0MB");
    }
}
