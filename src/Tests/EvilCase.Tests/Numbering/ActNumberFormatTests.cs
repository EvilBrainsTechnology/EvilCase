using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class ActNumberFormatTests
{
    private static readonly DateOnly ActDay = new(2026, 8, 12);

    private const string CaseNumber = "EC/20260807-001";

    [Test]
    public void ANumberIsTheCaseTheDayAndThreeDigits()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.Compose(CaseNumber, ActDay, 1), Is.EqualTo("EC/20260807-001/20260812-001"), "an act number is the case number, the day and a three-digit sequence");
            Assert.That(ActNumberFormat.Prefix(CaseNumber, ActDay), Is.EqualTo("EC/20260807-001/20260812-"), "the day prefix stops right before the sequence");
        }
    }

    [Test]
    public void ASequencePastThreeDigitsGrowsADigit()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.Compose(CaseNumber, ActDay, 1000), Is.EqualTo("EC/20260807-001/20260812-1000"), "the sequence grows past three digits rather than wrapping");
            Assert.That(
                ActNumberFormat.Parse("EC/20260807-001/20260812-1000"),
                Is.EqualTo(new ActNumberParts(CaseNumber, ActDay, 1000)),
                "a grown sequence still parses back");
        }
    }

    [Test]
    public void NextFollowsTheDaysHighestInsideTheCase()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.Next(CaseNumber, ActDay, highest: null), Is.EqualTo("EC/20260807-001/20260812-001"), "a day without an act of its own starts at 001");
            Assert.That(ActNumberFormat.Next(CaseNumber, ActDay, "EC/20260807-001/20260812-007"), Is.EqualTo("EC/20260807-001/20260812-008"), "the next number is one past the day's highest");
            Assert.That(ActNumberFormat.Next(CaseNumber, ActDay, "EC/20260807-001/20260812-999"), Is.EqualTo("EC/20260807-001/20260812-1000"), "the sequence grows a digit rather than wrapping");
            Assert.That(ActNumberFormat.Next(CaseNumber, ActDay, "spis 7/2026"), Is.EqualTo("EC/20260807-001/20260812-001"), "a value outside the format counts into no sequence");
        }
    }

    [Test]
    public void ABackDatedActTakesTheNextFreeSequenceOfItsOwnDay()
    {
        Assert.That(
            ActNumberFormat.Next(CaseNumber, new DateOnly(2026, 8, 8), "EC/20260807-001/20260808-002"),
            Is.EqualTo("EC/20260807-001/20260808-003"),
            "the sequence follows the day in the number, not the day the act is entered");
    }

    [Test]
    public void ParseReadsBackWhatComposeWrote()
    {
        Assert.That(
            ActNumberFormat.Parse("EC/20260807-001/20260812-042"),
            Is.EqualTo(new ActNumberParts(CaseNumber, ActDay, 42)),
            "parse is the inverse of compose");
    }

    [Test]
    public void ParseThrowsOnAnythingElse()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => ActNumberFormat.Parse("EC/20260807-001/20260812-1"), Throws.TypeOf<FormatException>(), "a sequence under three digits is outside the format");
            Assert.That(() => ActNumberFormat.Parse("EC/20260807-001/20260812-0001"), Throws.TypeOf<FormatException>(), "a padded sequence does not read back to itself");
            Assert.That(() => ActNumberFormat.Parse("spis 7/2026"), Throws.TypeOf<FormatException>(), "a hand-written case number alone has no act tail");
            Assert.That(() => ActNumberFormat.Parse("EC/20260807-001/20261312-001"), Throws.TypeOf<FormatException>(), "month 13 is not a day");
            Assert.That(() => ActNumberFormat.Parse(""), Throws.TypeOf<FormatException>(), "an empty value is not an act number");
        }
    }

    [Test]
    public void ParseOrDefaultReturnsNullOnAnythingElse()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.ParseOrDefault("EC/20260807-001/20260812-1"), Is.Null);
            Assert.That(ActNumberFormat.ParseOrDefault("EC/20260807-001/20260812-0001"), Is.Null);
            Assert.That(ActNumberFormat.ParseOrDefault("spis 7/2026"), Is.Null);
            Assert.That(ActNumberFormat.ParseOrDefault("EC/20260807-001/20261312-001"), Is.Null);
            Assert.That(ActNumberFormat.ParseOrDefault(""), Is.Null);
            Assert.That(ActNumberFormat.ParseOrDefault("EC/20260807-001/20260812-001"), Is.Not.Null, "a well-formed number does not return null");
        }
    }

    [Test]
    public void TheCaseNumberIsWhateverStandsBeforeTheDay()
    {
        Assert.That(
            ActNumberFormat.Parse("EC/20260807-001/20260812-001"),
            Is.EqualTo(new ActNumberParts("EC/20260807-001", ActDay, 1)),
            "the case number is everything before the act's own day and sequence");
    }

    [Test]
    public void AHandWrittenCaseNumberStillCarriesItsActs()
    {
        var composed = ActNumberFormat.Compose("spis 7/2026", ActDay, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composed, Is.EqualTo("spis 7/2026/20260812-001"), "a hand-written case number still prefixes its act numbers");
            Assert.That(ActNumberFormat.Parse(composed).CaseNumber, Is.EqualTo("spis 7/2026"), "the hand-written case number parses back out");
        }
    }
}
