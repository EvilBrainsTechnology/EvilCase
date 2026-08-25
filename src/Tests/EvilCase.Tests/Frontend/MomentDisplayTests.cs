using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MomentDisplayTests
{
    [Test]
    public void AMomentReadsTheCzechWayWithoutALeadingZeroInTheHour()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(MomentDisplay.Text(new DateTime(2026, 3, 9, 8, 5, 0, DateTimeKind.Local)), Is.EqualTo("9. 3. 2026 8:05"));
            Assert.That(MomentDisplay.Text(new DateTime(2026, 12, 24, 17, 30, 0, DateTimeKind.Local)), Is.EqualTo("24. 12. 2026 17:30"));
        }
    }

    [Test]
    public void TheFormatDoesNotFollowTheMachinesCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        try
        {
            Assert.That(
                MomentDisplay.Text(new DateTime(2026, 3, 9, 8, 5, 0, DateTimeKind.Local)),
                Is.EqualTo("9. 3. 2026 8:05"),
                "one place decides how a moment is rendered");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
