using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class CaseNumberFormatTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public void ComposePadsToThreeDigitsAndOverflowAddsOne()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.Compose(Day, sequence: 1), Is.EqualTo("EC/20260807-001"));
            Assert.That(CaseNumberFormat.Compose(Day, sequence: 42), Is.EqualTo("EC/20260807-042"));
            Assert.That(CaseNumberFormat.Compose(Day, sequence: 999), Is.EqualTo("EC/20260807-999"));
            Assert.That(CaseNumberFormat.Compose(Day, sequence: 1000), Is.EqualTo("EC/20260807-1000"), "overflow past three digits adds a digit");
        }
    }

    [Test]
    public void ParseReadsBackWhatComposeWrote()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var sequence in new[] { 1, 999, 1000 })
            {
                Assert.That(
                    CaseNumberFormat.Parse(CaseNumberFormat.Compose(Day, sequence)),
                    Is.EqualTo(new CaseNumberParts(Day, sequence)),
                    "parse reads back what compose wrote");
            }
        }
    }

    [Test]
    public void AValueOutsideTheFormatDoesNotParse()
    {
        string?[] values =
        [
            null,
            "",
            "EC/20260807-01",
            "EC/2026087-001",
            "ec/20260807-001",
            "EC/20260807-0001",
            "EC/20260807-000",
            "EC/20260231-001",
            "XX/20260807-001",
            "EC/20260807-001/20260812-001",
            "EC/20260807-001 ",
            "5 A 12/2026",
        ];

        using (Assert.EnterMultipleScope())
        {
            foreach (var value in values)
            {
                Assert.That(CaseNumberFormat.Parse(value), Is.Null, $"'{value}' is outside the format");
                Assert.That(CaseNumberFormat.IsValid(value), Is.False, $"'{value}' is outside the format");
            }
        }
    }

    [Test]
    public void NextSequenceIsOneMoreThanTheDaysHighest()
    {
        var next = CaseNumberFormat.NextSequence(Day, ["EC/20260807-001", "EC/20260807-003"]);

        Assert.That(next, Is.EqualTo(4));
    }

    [Test]
    public void ABackDatedDayStartsAtItsOwnNextSequence()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.NextSequence(Day, []), Is.EqualTo(1));
            Assert.That(
                CaseNumberFormat.NextSequence(Day, ["EC/20260808-005"]),
                Is.EqualTo(1),
                "a back-dated case takes the next free sequence of its own day");
        }
    }

    [Test]
    public void AHandWrittenNumberOutsideTheFormatDoesNotCount()
    {
        var next = CaseNumberFormat.NextSequence(Day, ["EC/20260807-001", "5 A 12/2026", "EC/20260807-0007", ""]);

        Assert.That(next, Is.EqualTo(2));
    }

    [Test]
    public void TheDayOverflowsPastNineHundredNinetyNine()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.NextSequence(Day, ["EC/20260807-999"]), Is.EqualTo(1000));
            Assert.That(CaseNumberFormat.Compose(Day, sequence: 1000), Is.EqualTo("EC/20260807-1000"));
            Assert.That(CaseNumberFormat.NextSequence(Day, ["EC/20260807-1000"]), Is.EqualTo(1001));
        }
    }

    [Test]
    public void DayPrefixIsWhatEveryNumberOfTheDayStartsWith()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.DayPrefix(Day), Is.EqualTo("EC/20260807-"));
            Assert.That(
                CaseNumberFormat.Compose(Day, sequence: 1).StartsWith(CaseNumberFormat.DayPrefix(Day), StringComparison.Ordinal),
                Is.True);
        }
    }

    [Test]
    public void ComposeRefusesASequenceBelowOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CaseNumberFormat.Compose(Day, sequence: 0));
}
