using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CzechTypographyTests
{
    [TestCase("Cesta k soudu", "Cesta k soudu")]
    [TestCase("V lese a v poli", "V lese a v poli")]
    [TestCase("(s tímto)", "(s tímto)")]
    [TestCase("121 km/h v úseku 110", "121 km/h v úseku 110")]
    [TestCase("Lhůta do 28. 5. 2025", "Lhůta do 28. 5. 2025")]
    [TestCase("podle § 125", "podle § 125")]
    [TestCase("Vzorov – 121", "Vzorov – 121")]
    [TestCase("Pokuta 2 000 Kč", "Pokuta 2 000 Kč")]
    [TestCase("1 250 000", "1 250 000")]
    public void ALineNeverBreaksWhereCzechTypographyForbidsIt(string text, string expected)
    {
        Assert.That(CzechTypography.Text(text), Is.EqualTo(expected));
    }

    [TestCase("Odvolání proti rozhodnutí")]
    [TestCase("Spis EC/20250528-001")]
    [TestCase("ok")]
    [TestCase("PSČ 101 00")]
    public void AnyOtherSpaceStaysBreakable(string text)
    {
        Assert.That(CzechTypography.Text(text), Is.EqualTo(text));
    }
}
