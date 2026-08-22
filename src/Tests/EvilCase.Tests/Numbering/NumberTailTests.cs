using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberTailTests
{
    [Test]
    public void ParseReadsTheDayAndThePlaceInIt()
    {
        var parts = NumberTail.Parse("20260807-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parts.Date, Is.EqualTo(new DateOnly(2026, 8, 7)), "the first eight digits are the day");
            Assert.That(parts.Sequence, Is.EqualTo(1), "the digits after the dash are the sequence");
        }
    }

    [Test]
    public void ASequenceThatGrewADigitStillParses()
    {
        Assert.That(NumberTail.Parse("20260807-1000").Sequence, Is.EqualTo(1000), "a sequence past three digits still parses");
    }

    [Test]
    public void ParseRefusesWhatIsNotATail()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => NumberTail.Parse("20260807-01"), Throws.TypeOf<FormatException>(), "a sequence under three digits is outside the format");
            Assert.That(() => NumberTail.Parse("2026-08-07-001"), Throws.TypeOf<FormatException>(), "the date carries no dashes of its own");
            Assert.That(() => NumberTail.Parse("20260807-abc"), Throws.TypeOf<FormatException>(), "the sequence is digits only");
            Assert.That(() => NumberTail.Parse("20260807001"), Throws.TypeOf<FormatException>(), "the dash between the day and the sequence is required");
            Assert.That(() => NumberTail.Parse("20261307-001"), Throws.TypeOf<FormatException>(), "month 13 is not a day");
        }
    }

    [Test]
    public void ParseOrDefaultAnswersWithNullInsteadOfThrowing()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberTail.ParseOrDefault(tail: null), Is.Null);
            Assert.That(NumberTail.ParseOrDefault(""), Is.Null);
            Assert.That(NumberTail.ParseOrDefault("   "), Is.Null);
            Assert.That(NumberTail.ParseOrDefault("20260807-abc"), Is.Null);
            Assert.That(NumberTail.ParseOrDefault("20260807-001"), Is.Not.Null, "a well-formed tail does not return null");
        }
    }

    [Test]
    public void ComposeAndParseAreEachOthersInverse()
    {
        var composed = NumberTail.Compose(new DateOnly(2026, 8, 7), 7);
        var parts = NumberTail.Parse(composed);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composed, Is.EqualTo("20260807-007"), "compose pads the sequence to three digits");
            Assert.That(parts.Date, Is.EqualTo(new DateOnly(2026, 8, 7)), "parse is the inverse of compose for the date");
            Assert.That(parts.Sequence, Is.EqualTo(7), "parse is the inverse of compose for the sequence");
        }
    }
}
