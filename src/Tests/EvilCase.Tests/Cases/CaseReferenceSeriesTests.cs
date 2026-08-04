using EvilBrains.EvilCase.Business.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseReferenceSeriesTests
{
    private const string Default = "ECYYYYMMDD-XXX";

    private static readonly DateOnly SecondOfAugust = new(2026, 8, 2);

    [Test]
    public void TheFirstCaseOfADayIsOne()
    {
        var next = CaseReferenceSeries.NextCounter(Default, SecondOfAugust, []);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(next, Is.EqualTo(1));
            Assert.That(CaseReferenceSeries.Format(Default, SecondOfAugust, next), Is.EqualTo("EC20260802-001"));
            Assert.That(CaseReferenceSeries.Prefix(Default, SecondOfAugust), Is.EqualTo("EC20260802-"), "which is what a query matches on");
        }
    }

    [Test]
    public void TheCounterContinuesPastWhatIsAlreadyTaken()
    {
        string[] taken = ["EC20260802-001", "EC20260802-002", "EC20260802-007"];

        var next = CaseReferenceSeries.NextCounter(Default, SecondOfAugust, taken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(next, Is.EqualTo(8), "one past the highest, not one past the count — a gap stays a gap");
            Assert.That(CaseReferenceSeries.Format(Default, SecondOfAugust, next), Is.EqualTo("EC20260802-008"));
        }
    }

    [Test]
    public void TheCounterStartsOverTheNextDay()
    {
        string[] yesterday = ["EC20260802-001", "EC20260802-042"];

        var next = CaseReferenceSeries.NextCounter(Default, SecondOfAugust.AddDays(1), yesterday);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(next, Is.EqualTo(1));
            Assert.That(CaseReferenceSeries.Format(Default, SecondOfAugust.AddDays(1), next), Is.EqualTo("EC20260803-001"));
        }
    }

    [Test]
    public void ADayPastItsWidthGrowsRatherThanRepeating()
    {
        string[] taken = ["EC20260802-999"];

        var next = CaseReferenceSeries.NextCounter(Default, SecondOfAugust, taken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(next, Is.EqualTo(1000));
            Assert.That(CaseReferenceSeries.Format(Default, SecondOfAugust, next), Is.EqualTo("EC20260802-1000"), "a repeated mark would be far worse than an uneven one");
        }
    }

    [Test]
    public void MarksFromAnotherSeriesAreIgnoredRatherThanRefused()
    {
        string[] taken = ["EC20260802-003", "OLD-2025-17", "EC20260801-900", "EC20260802-nonsense"];

        var next = CaseReferenceSeries.NextCounter(Default, SecondOfAugust, taken);

        Assert.That(next, Is.EqualTo(4), "a format can change, and yesterday's marks are not wrong for having been made under the old one");
    }

    [Test]
    public void AFormatIsLiteralTextAroundItsTokens()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseReferenceSeries.Format("SPIS/YYYY/XXXX", SecondOfAugust, 7), Is.EqualTo("SPIS/2026/0007"), "the width comes from how many X there are");
            Assert.That(CaseReferenceSeries.Format("X-DD.MM.YYYY", SecondOfAugust, 5), Is.EqualTo("5-02.08.2026"), "the counter need not come last");
            Assert.That(CaseReferenceSeries.CounterOf("SPIS/YYYY/XXXX", SecondOfAugust, "SPIS/2026/0007"), Is.EqualTo(7));
        }
    }

    [Test]
    public void AFormatWithNoCounterIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => CaseReferenceSeries.Format("EC-YYYYMMDD", SecondOfAugust, 1), Throws.ArgumentException, "a series with no counter would repeat every day");
            Assert.That(() => CaseReferenceSeries.Format(Default, SecondOfAugust, 0), Throws.InstanceOf<ArgumentOutOfRangeException>(), "counting starts at one");
        }
    }
}
