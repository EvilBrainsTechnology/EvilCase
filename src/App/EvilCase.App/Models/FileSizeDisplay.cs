namespace EvilBrains.EvilCase.App.Models;

public static class FileSizeDisplay
{
    private const long Kilobyte = 1024;

    private const long Megabyte = 1024 * Kilobyte;

    public static string Text(in long sizeBytes)
    {
        if (sizeBytes < Kilobyte)
            return string.Create(CultureInfo.InvariantCulture, $"{sizeBytes} B");

        if (sizeBytes < Megabyte)
            return string.Create(CultureInfo.InvariantCulture, $"{sizeBytes / (double)Kilobyte:0.#} kB");

        return string.Create(CultureInfo.InvariantCulture, $"{sizeBytes / (double)Megabyte:0.#} MB");
    }
}
