using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
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
    public void TheSequenceIsPaddedToThreeDigitsAndTheThousandthGrowsOutOfTheSort()
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
            Assert.That(issued, Is.Ordered.Using<string>(StringComparer.Ordinal), "the padding is what makes the first thousand of a series sort as text");
            Assert.That(NumberPattern.Format("{seq}", Date, 1000), Is.EqualTo("1000"), "the thousandth number of a series grows rather than wrapping");
            Assert.That(
                StringComparer.Ordinal.Compare(NumberPattern.Format("{seq}", Date, 1000), "999"),
                Is.LessThan(0),
                "and sorts in front of the one before it, which a yearly series reaches routinely — docs/product/vision.md says so");
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
            Assert.That(NumberPattern.Format("EC/{unknown}/{seq}", Date, 7), Is.EqualTo("EC/{unknown}/007"), "formatting writes an unknown placeholder out; refusing it is validation's job");
            Assert.That(NumberPattern.Format("{case-number}/{seq}", Date, 7), Is.EqualTo("/007"), "a case pattern naming the case number has none to write");
        }
    }

    [Test]
    public void TheDefaultsAreUsable()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate(NumberingDefaults.CaseNumberPattern, NumberPatternKind.CaseNumber), Is.Null);
            Assert.That(NumberPattern.Validate(NumberingDefaults.ActNumberPattern, NumberPatternKind.ActNumber), Is.Null);
        }
    }

    [Test]
    public void APatternThePlaceholdersDoNotCoverIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{quarter}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.UnknownPlaceholder));
            Assert.That(NumberPattern.Validate("EC-{year}-{seq", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.UnknownPlaceholder), "a brace with no partner is as unusable as an unknown name");
            Assert.That(NumberPattern.Validate("EC-{year}{month}{day}/{case-number}-{seq}", NumberPatternKind.ActNumber), Is.Null, "all five names are known");
        }
    }

    [Test]
    public void OnlyAnActPatternMayNameTheCaseNumber()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                NumberPattern.Validate("{case-number}EC-{seq}", NumberPatternKind.CaseNumber),
                Is.EqualTo(NumberPatternError.CaseNumberOutsideAnActPattern),
                "a placeholder in the wrong field is not one the application does not know, and the screen says something else about each");
            Assert.That(NumberPattern.Format("{case-number}EC-{seq}", Date, 1), Is.EqualTo("EC-001"), "what the refusal is about");
            Assert.That(NumberPattern.Validate("{case-number}EC-{seq}", NumberPatternKind.ActNumber), Is.Null);
        }
    }

    [Test]
    public void APatternWithoutTheSequenceIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{year}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.NoSequence), "without {seq} every case gets the same number");
            Assert.That(NumberPattern.Validate("EC-{year}-{seq}", NumberPatternKind.CaseNumber), Is.Null);
        }
    }

    /// <summary>
    /// The widest a pattern ever writes is what has to fit: a series counted to <c>int.MaxValue</c> is
    /// ten digits, and <c>{case-number}</c> can be a case number filling its own column.
    /// </summary>
    [Test]
    public void APatternThatWouldNotFitTheColumnItIsStoredInIsRefused()
    {
        var caseBudget = Case.CaseNumberLength - "2147483647".Length;
        var actBudget = Act.ActNumberLength - "2147483647".Length - Case.CaseNumberLength;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate(new string('X', caseBudget) + "{seq}", NumberPatternKind.CaseNumber), Is.Null);
            Assert.That(
                NumberPattern.Validate(new string('X', caseBudget + 1) + "{seq}", NumberPatternKind.CaseNumber),
                Is.EqualTo(NumberPatternError.TooLongForItsColumn),
                "a pattern that overflows its column fails on the insert, with the {seq} it took already gone");
            Assert.That(NumberPattern.Validate(new string('X', actBudget) + "{case-number}{seq}", NumberPatternKind.ActNumber), Is.Null);
            Assert.That(
                NumberPattern.Validate(new string('X', actBudget + 1) + "{case-number}{seq}", NumberPatternKind.ActNumber),
                Is.EqualTo(NumberPatternError.TooLongForItsColumn),
                "an act number carries a whole case number, so it runs out of its own column that much sooner");
        }
    }

    /// <summary>
    /// The series counts within the finest part the pattern names, so a pattern that writes that part
    /// without the coarser ones repeats itself: <c>EC-{day}-{seq}</c> issued <c>EC-05-001</c> on the
    /// fifth of August and again on the fifth of September.
    /// </summary>
    [Test]
    public void APatternThatWouldWriteOneNumberTwiceIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Validate("EC-{day}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod));
            Assert.That(NumberPattern.Validate("EC-{month}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod));
            Assert.That(NumberPattern.Validate("EC-{month}{day}-{seq}", NumberPatternKind.CaseNumber), Is.EqualTo(NumberPatternError.RepeatingPeriod), "the year is missing whichever finer part is named");
            Assert.That(NumberPattern.Validate("EC-{year}{month}-{seq}", NumberPatternKind.CaseNumber), Is.Null, "a month written under its year counts monthly and stays distinct");
            Assert.That(NumberPattern.Validate("EC-{seq}", NumberPatternKind.CaseNumber), Is.Null, "one series forever writes each number once");
        }
    }
}
