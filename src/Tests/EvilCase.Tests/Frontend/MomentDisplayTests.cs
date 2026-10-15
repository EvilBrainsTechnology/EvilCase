using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MomentDisplayTests
{
    [Test]
    public void AMomentReadsTheCzechWayWithoutALeadingZeroInTheHour()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(MomentDisplay.Text(new DateTime(2026, 3, 9, 8, 5, 0, DateTimeKind.Local)), Is.EqualTo("9.\u00A03.\u00A02026 8:05"));
            Assert.That(MomentDisplay.Text(new DateTime(2026, 12, 24, 17, 30, 0, DateTimeKind.Local)), Is.EqualTo("24.\u00A012.\u00A02026 17:30"));
        }
    }

    [Test]
    public void TodayAndYesterdayReadRelativelyAndEveryOtherMomentInFull()
    {
        var now = new DateTime(2026, 10, 13, 22, 0, 0, DateTimeKind.Local);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(MomentDisplay.Relative(new DateTime(2026, 10, 13, 21, 45, 0, DateTimeKind.Local), now), Is.EqualTo("dnes 21:45"));
            Assert.That(MomentDisplay.Relative(new DateTime(2026, 10, 12, 9, 10, 0, DateTimeKind.Local), now), Is.EqualTo("včera 9:10"));
            Assert.That(MomentDisplay.Relative(new DateTime(2026, 10, 11, 9, 10, 0, DateTimeKind.Local), now), Is.EqualTo("11.\u00A010.\u00A02026 9:10"));
            Assert.That(MomentDisplay.Relative(new DateTime(2026, 10, 14, 8, 0, 0, DateTimeKind.Local), now), Is.EqualTo("14.\u00A010.\u00A02026 8:00"), "a moment ahead of now never reads as today");
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
                Is.EqualTo("9.\u00A03.\u00A02026 8:05"),
                "one place decides how a moment is rendered");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
