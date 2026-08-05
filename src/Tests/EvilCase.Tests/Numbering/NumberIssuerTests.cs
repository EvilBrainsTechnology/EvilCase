using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Tests.Auth;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberIssuerTests
{
    private FakeNumberSequenceAllocator sequences = null!;

    private TestTimeProvider time = null!;

    [SetUp]
    public void SetUp()
    {
        this.sequences = new FakeNumberSequenceAllocator();
        this.time = new TestTimeProvider(new DateTime(2026, 8, 4, 9, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task EveryCaseTakesTheNextNumberOfTheDaysSeries()
    {
        var issuer = this.Issuer();

        var first = await issuer.IssueCaseNumber();
        var second = await issuer.IssueCaseNumber();

        string[] expected = ["case:20260804", "case:20260804"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-20260804-001"));
            Assert.That(second, Is.EqualTo("EC-20260804-002"), "a number once issued is never issued again");
            Assert.That(this.sequences.Scopes, Is.EqualTo(expected), "sub-case and case take from one series");
        }
    }

    [Test]
    public async Task ANewPeriodStartsTheCountAgain()
    {
        var issuer = this.Issuer();

        var today = await issuer.IssueCaseNumber();
        this.time.Advance(TimeSpan.FromDays(1));
        var tomorrow = await issuer.IssueCaseNumber();

        string[] expected = ["case:20260804", "case:20260805"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(today, Is.EqualTo("EC-20260804-001"));
            Assert.That(tomorrow, Is.EqualTo("EC-20260805-001"), "a daily pattern counts within the day it names");
            Assert.That(this.sequences.Scopes, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task AYearlyPatternKeepsCountingAcrossTheDays()
    {
        var issuer = this.Issuer(caseNumberPattern: "EC-{year}-{seq}");

        var first = await issuer.IssueCaseNumber();
        this.time.Advance(TimeSpan.FromDays(1));
        var second = await issuer.IssueCaseNumber();

        string[] expected = ["case:2026", "case:2026"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-2026-001"));
            Assert.That(second, Is.EqualTo("EC-2026-002"));
            Assert.That(this.sequences.Scopes, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task AnActCountsWithinItsOwnCase()
    {
        var issuer = this.Issuer();

        var first = await issuer.IssueActNumber(caseId: 1, "EC-20260804-001");
        var second = await issuer.IssueActNumber(caseId: 1, "EC-20260804-001");
        var elsewhere = await issuer.IssueActNumber(caseId: 2, "EC-20260804-002");

        string[] expected = ["act:1:20260804", "act:1:20260804", "act:2:20260804"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-20260804-001-20260804-001"));
            Assert.That(second, Is.EqualTo("EC-20260804-001-20260804-002"));
            Assert.That(elsewhere, Is.EqualTo("EC-20260804-002-20260804-001"), "another case counts from its own start");
            Assert.That(this.sequences.Scopes, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task TheDayIsPraguesAndNotUtcs()
    {
        this.time = new TestTimeProvider(new DateTime(2026, 8, 4, 22, 30, 0, DateTimeKind.Utc));

        var number = await this.Issuer().IssueCaseNumber();

        string[] expected = ["case:20260805"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(number, Is.EqualTo("EC-20260805-001"), "half past midnight in Prague is already the fifth, whatever UTC still says");
            Assert.That(this.sequences.Scopes, Is.EqualTo(expected), "the series counts within a Prague day");
        }
    }

    [Test]
    public async Task WinterKeepsTheSameDayAsUtcAnHourLonger()
    {
        this.time = new TestTimeProvider(new DateTime(2026, 1, 4, 22, 30, 0, DateTimeKind.Utc));

        var number = await this.Issuer().IssueCaseNumber();

        Assert.That(number, Is.EqualTo("EC-20260104-001"), "the offset is the one in force on the day, not the summer's two hours");
    }

    [Test]
    public async Task AnActOfAHandNumberedCaseIsWrittenUnderThatNumber()
    {
        var number = await this.Issuer().IssueActNumber(caseId: 7, "OLD-2019/16");

        Assert.That(number, Is.EqualTo("OLD-2019/16-20260804-001"), "the case's own number is used whether it was issued here or typed in");
    }

    [Test]
    public void APatternThatCannotWorkIsRefusedRatherThanIssuedFrom()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(async () => await this.Issuer(caseNumberPattern: "EC-{year}").IssueCaseNumber(), Throws.InstanceOf<InvalidOperationException>(), "without {seq} every case would take the same number");
            Assert.That(async () => await this.Issuer(caseNumberPattern: "EC-{day}-{seq}").IssueCaseNumber(), Throws.InstanceOf<InvalidOperationException>(), "the fifth of September would take the numbers of the fifth of August");
            Assert.That(this.sequences.Scopes, Is.Empty, "a pattern that cannot work burns no number either");
        }
    }

    private NumberIssuer Issuer(string? caseNumberPattern = null) =>
        new(new FakeNumberingSettingsReader(caseNumberPattern), this.sequences, this.time);
}
