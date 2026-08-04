using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberPatternTests
{
    private static readonly DateOnly Date = new(2026, 8, 4);

    [Test]
    public void TheDefaultPatternsWriteTheNumbersTheVisionShows()
    {
        var caseNumber = NumberPattern.Format(NumberingDefaults.CaseNumberPattern, Date, 1);
        var actNumber = NumberPattern.Format(NumberingDefaults.ActNumberPattern, new DateOnly(2026, 8, 5), 2, caseNumber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseNumber, Is.EqualTo("EC-20260804-001"));
            Assert.That(actNumber, Is.EqualTo("EC-20260804-001-20260805-002"), "an act number is written under the case's own number");
        }
    }

    [Test]
    public void TheSequenceIsPaddedToThreeDigitsSoNumbersSort()
    {
        string[] issued =
        [
            NumberPattern.Format("{seq}", Date, 1),
            NumberPattern.Format("{seq}", Date, 42),
            NumberPattern.Format("{seq}", Date, 999),
        ];

        string[] expected = ["001", "042", "999"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(issued, Is.EqualTo(expected));
            Assert.That(issued, Is.Ordered.Using<string>(StringComparer.Ordinal), "the padding is what makes one series sort as text");
            Assert.That(NumberPattern.Format("{seq}", Date, 1000), Is.EqualTo("1000"), "the thousandth number of a series grows rather than wrapping");
        }
    }

    [Test]
    public void ASeriesCountsWithinTheFinestPeriodThePatternNames()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.PeriodKey("EC-{year}{month}{day}-{seq}", Date), Is.EqualTo("20260804"), "a pattern naming the day counts daily");
            Assert.That(NumberPattern.PeriodKey("EC-{year}{month}-{seq}", Date), Is.EqualTo("202608"), "one naming the month counts monthly");
            Assert.That(NumberPattern.PeriodKey("EC-{year}/{seq}", Date), Is.EqualTo("2026"), "one naming only the year counts yearly");
            Assert.That(NumberPattern.PeriodKey("EC-{seq}", Date), Is.Empty, "one naming no date part counts on and on");
        }
    }

    [Test]
    public void TwoDaysOfOneYearAreTwoSeriesAndTwoMonthsOfOneYearAreNot()
    {
        var later = new DateOnly(2026, 8, 5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.PeriodKey("{year}{month}{day}{seq}", later), Is.Not.EqualTo(NumberPattern.PeriodKey("{year}{month}{day}{seq}", Date)));
            Assert.That(NumberPattern.PeriodKey("{year}{seq}", later), Is.EqualTo(NumberPattern.PeriodKey("{year}{seq}", Date)), "a yearly pattern keeps counting across days");
        }
    }

    [Test]
    public void WhatThePatternDoesNotNameIsWrittenOutAsItStands()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.NamesSequence("EC-{year}"), Is.False);
            Assert.That(NumberPattern.NamesSequence("EC-{year}-{seq}"), Is.True);
            Assert.That(NumberPattern.Format("EC/{unknown}/{seq}", Date, 7), Is.EqualTo("EC/{unknown}/007"), "an unknown placeholder is text, not an error");
            Assert.That(NumberPattern.Format("{case-number}/{seq}", Date, 7), Is.EqualTo("/007"), "a case pattern naming the case number has none to write");
        }
    }
}
