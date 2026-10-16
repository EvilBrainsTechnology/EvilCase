using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DateDisplayTests
{
    [Test]
    public void ADateReadsTheCzechWayWithoutLeadingZeros()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DateDisplay.Text(new DateOnly(2026, 3, 9)), Is.EqualTo("9. 3. 2026"));
            Assert.That(DateDisplay.Text(new DateOnly(2026, 12, 24)), Is.EqualTo("24. 12. 2026"));
        }
    }

    [Test]
    public void TheFormatDoesNotFollowTheMachinesCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        try
        {
            Assert.That(DateDisplay.Text(new DateOnly(2026, 3, 9)), Is.EqualTo("9. 3. 2026"), "one place decides how a date is rendered");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
