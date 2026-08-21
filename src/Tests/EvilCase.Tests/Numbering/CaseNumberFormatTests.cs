using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class CaseNumberFormatTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public void ANumberIsTheDayAndThreeDigits()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.Compose(Day, 1), Is.EqualTo("EC/20260807-001"), "a case number is EC, the day and a three-digit sequence");
            Assert.That(CaseNumberFormat.DayPrefix(Day), Is.EqualTo("EC/20260807-"), "the day prefix stops right before the sequence");
        }
    }

    [Test]
    public void ASequencePastThreeDigitsGrowsADigit()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.Compose(Day, 1000), Is.EqualTo("EC/20260807-1000"), "the sequence grows past three digits rather than wrapping");
            Assert.That(CaseNumberFormat.Parse("EC/20260807-1000"), Is.EqualTo(new CaseNumberParts(Day, 1000)), "a grown sequence still parses back");
        }
    }

    [Test]
    public void ParseReadsBackWhatComposeWrote() =>
        Assert.That(CaseNumberFormat.Parse("EC/20260807-042"), Is.EqualTo(new CaseNumberParts(Day, 42)), "parse is the inverse of compose");

    [Test]
    public void ParseThrowsOnAnythingElse()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => CaseNumberFormat.Parse("EC/20260807-1"), Throws.TypeOf<FormatException>(), "a sequence under three digits is outside the format");
            Assert.That(() => CaseNumberFormat.Parse("EC/20260807-0001"), Throws.TypeOf<FormatException>(), "a padded sequence does not read back to itself");
            Assert.That(() => CaseNumberFormat.Parse("XX/20260807-001"), Throws.TypeOf<FormatException>(), "the prefix is fixed");
            Assert.That(() => CaseNumberFormat.Parse("EC/20261307-001"), Throws.TypeOf<FormatException>(), "month 13 is not a day");
            Assert.That(() => CaseNumberFormat.Parse(""), Throws.TypeOf<FormatException>(), "an empty value is not a case number");
        }
    }

    [Test]
    public void ParseOrDefaultReturnsNullOnAnythingElse()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseNumberFormat.ParseOrDefault("EC/20260807-1"), Is.Null);
            Assert.That(CaseNumberFormat.ParseOrDefault("EC/20260807-0001"), Is.Null);
            Assert.That(CaseNumberFormat.ParseOrDefault("XX/20260807-001"), Is.Null);
            Assert.That(CaseNumberFormat.ParseOrDefault("EC/20261307-001"), Is.Null);
            Assert.That(CaseNumberFormat.ParseOrDefault(""), Is.Null);
            Assert.That(CaseNumberFormat.ParseOrDefault("EC/20260807-001"), Is.Not.Null, "a well-formed number does not return null");
        }
    }

    [Test]
    public void TheNextSequenceSkipsWhatDoesNotFitTheFormatOrTheDay()
    {
        string[] numbers = ["EC/20260807-001", "EC/20260807-003", "spis 7/2026", "EC/20260808-009"];

        Assert.That(CaseNumberFormat.NextSequence(Day, numbers), Is.EqualTo(4), "the next sequence is one past the day's highest, ignoring other days and hand-written values");
    }

    [Test]
    public void AnEmptyDayStartsAtOne() =>
        Assert.That(CaseNumberFormat.NextSequence(Day, []), Is.EqualTo(1), "the first number of a day is sequence one");
}
