using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class ActNumberFormatTests
{
    private const string CaseNumber = "EC/20260807-001";

    private static readonly DateOnly Day = new(2026, 8, 12);

    [Test]
    public void ComposeHangsTheDayAndSequenceOnTheCaseNumber()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.Compose(CaseNumber, Day, sequence: 1), Is.EqualTo("EC/20260807-001/20260812-001"));
            Assert.That(ActNumberFormat.Compose(CaseNumber, Day, sequence: 1000), Is.EqualTo("EC/20260807-001/20260812-1000"));
        }
    }

    [Test]
    public void ParseReadsBackWhatComposeWrote() =>
        Assert.That(
            ActNumberFormat.Parse(ActNumberFormat.Compose(CaseNumber, Day, sequence: 7)),
            Is.EqualTo(new ActNumberParts(CaseNumber, Day, 7)));

    [Test]
    public void AValueOutsideTheFormatDoesNotParse()
    {
        string?[] values =
        [
            null,
            "",
            "EC/20260807-001",
            "EC/20260807-001/20260812-01",
            "EC/20260807-001/20261332-001",
            "EC/2026087-001/20260812-001",
            "EC/20260807-0001/20260812-001",
            "EC/20260807-001/20260812-0001",
            "EC/20260807-001/20260812-001/20260813-001",
        ];

        using (Assert.EnterMultipleScope())
        {
            foreach (var value in values)
            {
                Assert.That(ActNumberFormat.Parse(value), Is.Null, $"'{value}' is outside the format");
                Assert.That(ActNumberFormat.IsValid(value), Is.False, $"'{value}' is outside the format");
            }
        }
    }

    [Test]
    public void NextSequenceCountsTheCasesDay()
    {
        string[] numbers =
        [
            "EC/20260807-001/20260812-001",
            "EC/20260807-001/20260812-002",
            "EC/20260807-001/20260813-001",
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActNumberFormat.NextSequence(new DateOnly(2026, 8, 12), numbers), Is.EqualTo(3));
            Assert.That(ActNumberFormat.NextSequence(new DateOnly(2026, 8, 13), numbers), Is.EqualTo(2));
            Assert.That(ActNumberFormat.NextSequence(new DateOnly(2026, 8, 14), numbers), Is.EqualTo(1));
        }
    }

    [Test]
    public void NumbersIssuedUnderARewrittenCaseNumberStillCount()
    {
        string[] numbers = ["EC/20260807-001/20260812-001", "EC/20260901-007/20260812-002"];

        Assert.That(
            ActNumberFormat.NextSequence(new DateOnly(2026, 8, 12), numbers),
            Is.EqualTo(3),
            "a rewritten case number leaves the numbers it already issued alone");
    }

    [Test]
    public void AHandWrittenNumberOutsideTheFormatDoesNotCount()
    {
        string[] numbers = ["EC/20260807-001/20260812-001", "MV-1234/2026", "EC/20260807-001/20260812-0009"];

        Assert.That(ActNumberFormat.NextSequence(Day, numbers), Is.EqualTo(2));
    }

    [Test]
    public void ComposeRefusesACaseNumberOutsideTheFormat() =>
        Assert.Throws<ArgumentException>(() => ActNumberFormat.Compose("5 A 12/2026", Day, sequence: 1));

    [Test]
    public void ComposeRefusesASequenceBelowOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ActNumberFormat.Compose(CaseNumber, Day, sequence: 0));
}
