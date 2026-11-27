using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberTailTests
{
    [Test]
    public void ParseReadsTheDayAndThePlaceInIt()
    {
        var parts = NumberTail.ParseOrDefault("20260807-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parts?.Date, Is.EqualTo(new DateOnly(2026, 8, 7)), "the first eight digits are the day");
            Assert.That(parts?.Sequence, Is.EqualTo(1), "the digits after the dash are the sequence");
        }
    }

    [Test]
    public void ASequenceThatGrewADigitStillParses()
    {
        Assert.That(NumberTail.ParseOrDefault("20260807-1000")?.Sequence, Is.EqualTo(1000), "a sequence past three digits still parses");
    }

    [Test]
    public void ParseOrDefaultReturnsNullOnWhatIsNotATail()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberTail.ParseOrDefault(tail: null), Is.Null);
            Assert.That(NumberTail.ParseOrDefault(""), Is.Null);
            Assert.That(NumberTail.ParseOrDefault("   "), Is.Null);
            Assert.That(NumberTail.ParseOrDefault("20260807-01"), Is.Null, "a sequence under three digits is outside the format");
            Assert.That(NumberTail.ParseOrDefault("2026-08-07-001"), Is.Null, "the date carries no dashes of its own");
            Assert.That(NumberTail.ParseOrDefault("20260807-abc"), Is.Null, "the sequence is digits only");
            Assert.That(NumberTail.ParseOrDefault("20260807001"), Is.Null, "the dash between the day and the sequence is required");
            Assert.That(NumberTail.ParseOrDefault("20261307-001"), Is.Null, "month 13 is not a day");
            Assert.That(NumberTail.ParseOrDefault("20260807-001"), Is.Not.Null, "a well-formed tail does not return null");
        }
    }

    [Test]
    public void ComposeAndParseAreEachOthersInverse()
    {
        var composed = NumberTail.Compose(new DateOnly(2026, 8, 7), 7);
        var parts = NumberTail.ParseOrDefault(composed);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composed, Is.EqualTo("20260807-007"), "compose pads the sequence to three digits");
            Assert.That(parts?.Date, Is.EqualTo(new DateOnly(2026, 8, 7)), "parse is the inverse of compose for the date");
            Assert.That(parts?.Sequence, Is.EqualTo(7), "parse is the inverse of compose for the sequence");
        }
    }
}
