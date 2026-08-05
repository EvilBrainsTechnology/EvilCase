using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The pattern read the other way round. The day is literal text by the time a series is built — the
/// day the number is being issued on — so the sequence begins and ends a fixed distance from the
/// number's two edges. The case number an act carries is not: its case is what the series is scoped by.
/// </summary>
public class NumberSeriesTests
{
    private static readonly DateOnly Date = new(2026, 8, 4);

    [Test]
    public void WhatThePatternWritesIsWhatTheSeriesReadsBack()
    {
        var series = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);

        int[] written = [1, 42, 999, 1000, int.MaxValue];
        var numbers = written.Select(sequence => NumberPattern.Format(NumberingDefaults.CaseNumberPattern, Date, sequence)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Prefix, Is.EqualTo("EC-20260804-"), "the prefix is what a query reads the candidate rows by");
            Assert.That(series.Highest(numbers), Is.EqualTo(int.MaxValue));
            Assert.That(numbers.Select(number => series.Highest([number])), Is.EqualTo(written), "every number of the series gives its own sequence back");
        }
    }

    /// <summary>
    /// A pattern's literal text is the operator's and a case number written into it is whatever was
    /// typed. Read as a pattern of its own, one underscore would read every case number of the same
    /// length instead.
    /// </summary>
    [Test]
    public void ThePrefixReachesTheQueryAsTextRatherThanAsWildcards()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberPattern.Series("A_C-{year}-{seq}", Date).LikePrefix, Is.EqualTo(@"A\_C-2026-%"));
            Assert.That(NumberPattern.Series(@"100%\ok-{year}{month}{day}/{case-number}-{seq}", Date).LikePrefix, Is.EqualTo(@"100\%\\ok-20260804/%"), "and the prefix stops where the case number starts");
        }
    }

    [Test]
    public void ASeriesWithNothingInItStartsAtZero()
    {
        var series = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);

        Assert.That(series.Highest([]), Is.Zero, "so the first number the pattern issues is one");
    }

    [Test]
    public void AnotherPeriodIsAnotherSeries()
    {
        var today = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(today.Highest(["EC-20260805-007"]), Is.Zero, "the day is written into the number, so another day's is not one of these");
            Assert.That(today.Highest(["EC-20260804-007"]), Is.EqualTo(7));
        }
    }

    /// <summary>
    /// A generated case number can be overwritten, and the acts numbered before that keep the text they
    /// were written under. An act series is scoped by the case, so the number inside an act's own is
    /// whatever its case was called at the time.
    /// </summary>
    [Test]
    public void AnActStaysInItsSeriesWhenItsCaseIsRenumbered()
    {
        var series = NumberPattern.Series(NumberingDefaults.ActNumberPattern, Date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Prefix, Is.Empty, "a pattern opening with its case number has no prefix to read by");
            Assert.That(
                series.Highest(["EC-20260804-001-20260804-047", "SP-99/2026-20260804-002"]),
                Is.EqualTo(47),
                "one case is one series, whatever number its acts were written under");
        }
    }

    /// <summary>
    /// A case number is a column the operator types into, control characters and all. The number the
    /// pattern wrote under it is one of its case's series like any other — read as anything but that,
    /// the case would number its next act over the top of the one before.
    /// </summary>
    [Test]
    public void ACaseNumberCarryingANewlineIsStillItsCasesSeries()
    {
        var series = NumberPattern.Series(NumberingDefaults.ActNumberPattern, Date);

        Assert.That(series.Highest(["A\nB-20260804-047"]), Is.EqualTo(47), "a case number is whatever was typed, and the series reads across all of it");
    }

    /// <summary>
    /// <c>$</c> matches at the end of the string or before a trailing newline, so it would count a
    /// number the pattern could not have written; <c>\z</c> is the end and nothing else.
    /// </summary>
    [Test]
    public void ATailTheSeriesDoesNotEndWithIsNotOfIt()
    {
        var series = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);
        var suffixed = NumberPattern.Series("{seq}/2026", Date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Highest(["EC-20260804-050\n"]), Is.Zero, "a newline on the end is not something the pattern writes");
            Assert.That(suffixed.Highest(["12345/2026x"]), Is.Zero, "and neither is anything past the tail the pattern ends with");
            Assert.That(suffixed.Highest(["12345/2026"]), Is.EqualTo(12345));
        }
    }

    /// <summary>
    /// The column holds marks somebody typed in, and nothing says which is which. One in the shape the
    /// pattern writes counts — reissuing it is what the unique index would refuse — and one of any
    /// other shape is invisible.
    /// </summary>
    [Test]
    public void OnlyWhatThePatternCouldHaveWrittenCounts()
    {
        var series = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);

        string[] stored = ["OLD-2019/16", "EC-20260804", "EC-20260804-", "EC-20260804-00", "EC-20260804-12x", "EC-20260804-021", " EC-20260804-777"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Highest(stored), Is.EqualTo(21), "the one mark in the shape the pattern writes is the one that counts");
            Assert.That(series.Highest(["EC-20260804-99999999999999"]), Is.Zero, "more digits than an int holds is not a number this application wrote");
        }
    }

    /// <summary>
    /// Changing a pattern rewrites nothing, so numbers of the pattern before it stay in the column. It
    /// is the day's own text that decides, not the shape: <c>EC-{day}{month}{year}-{seq}</c> writes
    /// eight digits like the pattern in force, and the 133 401 dates that do render as another date's
    /// <c>{year}{month}{day}</c> all need one of the two years between 0101 and 1231, so no pair this
    /// application could issue under collides. What the pattern in force could have written today is
    /// counted, whichever pattern wrote it, so a pattern change costs a skipped number, never a
    /// repeated one.
    /// </summary>
    [Test]
    public void ANumberOfTheOlderPatternCountsExactlyWhereTheNewOneCouldHaveWrittenIt()
    {
        var series = NumberPattern.Series(NumberingDefaults.CaseNumberPattern, Date);
        var reordered = NumberPattern.Format("EC-{day}{month}{year}-{seq}", Date, 9);
        var wider = NumberPattern.Format("EC-{year}{month}{day}-{seq:5}", Date, 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Highest([reordered]), Is.Zero, $"{reordered} is not what the pattern in force writes today");
            Assert.That(series.Highest([wider]), Is.EqualTo(42), $"{wider} is a number it could have written, so it counts rather than being handed out again");
            Assert.That(NumberPattern.Format(NumberingDefaults.CaseNumberPattern, Date, 43), Is.Not.EqualTo(wider), "and the number after it is one nothing holds");
        }
    }

    [Test]
    public void TheWidthIsAMinimumOnTheWayBackToo()
    {
        var series = NumberPattern.Series("EC-{year}-{seq:6}", Date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(series.Highest(["EC-2026-000001", "EC-2026-1000000"]), Is.EqualTo(1000000), "a series past its width keeps counting, so reading it back cannot stop at the width either");
            Assert.That(series.Highest(["EC-2026-001"]), Is.Zero, "and what is narrower than the width is not of this series");
        }
    }

    /// <summary>
    /// A <c>{seq}</c> of no fixed width next to other digits is what #206 left open. Where those digits
    /// are the day's they are written out before the series is built, so both ends of the sequence are
    /// fixed and there is nothing left to be greedy about; where they are a case number's, nothing
    /// fixes either end and <c>Validate</c> refuses the pattern instead.
    /// </summary>
    [Test]
    public void DigitsRightBesideTheSequenceStillReadBackExactly()
    {
        var leading = NumberPattern.Series("EC{year}{seq}", Date);
        var trailing = NumberPattern.Series("{seq}/2026", Date);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(leading.Prefix, Is.EqualTo("EC2026"));
            Assert.That(leading.Highest(["EC2026007", "EC20261234"]), Is.EqualTo(1234), "the prefix is fixed text, so the digits after it are all sequence");
            Assert.That(trailing.Prefix, Is.Empty, "a pattern opening with its sequence has no prefix to read by");
            Assert.That(trailing.Highest(["0012/2026", "12345/2026"]), Is.EqualTo(12345), "and the tail is fixed text, so the digits in front of it are all sequence");
        }
    }
}
