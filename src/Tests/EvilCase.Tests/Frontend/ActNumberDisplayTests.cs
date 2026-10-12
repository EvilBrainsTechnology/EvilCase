using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActNumberDisplayTests
{
    [TestCase("EC/20250528-001/20250902-001", "EC/20250528-001", "20250902-001")]
    [TestCase("EC/20250528-002/20250902-001", "EC/20250528-001", "EC/20250528-002/20250902-001")]
    [TestCase("MUVZ-117", "EC/20250528-001", "MUVZ-117")]
    [TestCase("EC/20250528-001/", "EC/20250528-001", "EC/20250528-001/")]
    [TestCase("EC/20250528-0011/20250902-001", "EC/20250528-001", "EC/20250528-0011/20250902-001")]
    public void TheCasePrefixFallsAwayOnlyWhereTheNumberCarriesIt(string actNumber, string caseNumber, string expected)
    {
        Assert.That(ActNumberDisplay.WithinCase(actNumber, caseNumber), Is.EqualTo(expected));
    }
}
