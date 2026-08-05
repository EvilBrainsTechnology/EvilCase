using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Tests.Auth;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberIssuerTests
{
    private static readonly TimeZoneInfo Prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private FakeIssuedNumberReader issued = null!;

    private FakeCaseNumberReader cases = null!;

    private TestTimeProvider time = null!;

    [SetUp]
    public void SetUp()
    {
        this.issued = new FakeIssuedNumberReader();
        this.cases = new FakeCaseNumberReader((1, "EC-20260804-001"), (2, "EC-20260804-002"), (7, "OLD-2019/16"));
        this.time = new TestTimeProvider(new DateTime(2026, 8, 4, 9, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task EveryCaseTakesTheNumberAfterTheHighestTheSeriesHolds()
    {
        var issuer = this.Issuer();

        var first = await this.IssueCaseNumber(issuer);
        var second = await this.IssueCaseNumber(issuer);

        string[] expected = ["EC-20260804-", "EC-20260804-"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-20260804-001"));
            Assert.That(second, Is.EqualTo("EC-20260804-002"), "the number just written is what the next one counts from");
            Assert.That(this.issued.Series, Is.EqualTo(expected), "every case reads the one series its pattern names");
        }
    }

    [Test]
    public async Task ANewPeriodStartsTheCountAgain()
    {
        var issuer = this.Issuer();

        var today = await this.IssueCaseNumber(issuer);
        this.time.Advance(TimeSpan.FromDays(1));
        var tomorrow = await this.IssueCaseNumber(issuer);

        string[] expected = ["EC-20260804-", "EC-20260805-"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(today, Is.EqualTo("EC-20260804-001"));
            Assert.That(tomorrow, Is.EqualTo("EC-20260805-001"), "a daily pattern counts within the day it names");
            Assert.That(this.issued.Series, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task AYearlyPatternKeepsCountingAcrossTheDays()
    {
        var issuer = this.Issuer(caseNumberPattern: "EC-{year}-{seq}");

        var first = await this.IssueCaseNumber(issuer);
        this.time.Advance(TimeSpan.FromDays(1));
        var second = await this.IssueCaseNumber(issuer);

        string[] expected = ["EC-2026-", "EC-2026-"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-2026-001"));
            Assert.That(second, Is.EqualTo("EC-2026-002"));
            Assert.That(this.issued.Series, Is.EqualTo(expected));
        }
    }

    /// <summary>
    /// The column holds marks somebody typed in beside the numbers this application issued, and nothing
    /// records which is which. A mark in the shape the pattern writes is therefore counted: the next
    /// number would otherwise be one the unique index refuses.
    /// </summary>
    [Test]
    public async Task AHandTypedMarkInTheShapeThePatternWritesCountsTowardsTheNext()
    {
        this.issued.KeepCaseNumber("EC-20260804-042");
        this.issued.KeepCaseNumber("OLD-2019/16");

        Assert.That(await this.IssueCaseNumber(this.Issuer()), Is.EqualTo("EC-20260804-043"), "what parses counts whoever typed it, and what does not is invisible");
    }

    [Test]
    public async Task AnActCountsWithinItsOwnCase()
    {
        var issuer = this.Issuer();

        var first = await this.IssueActNumber(issuer, caseId: 1);
        var second = await this.IssueActNumber(issuer, caseId: 1);
        var elsewhere = await this.IssueActNumber(issuer, caseId: 2);

        string[] expected = ["1:EC-20260804-001-20260804-", "1:EC-20260804-001-20260804-", "2:EC-20260804-002-20260804-"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-20260804-001-20260804-001"));
            Assert.That(second, Is.EqualTo("EC-20260804-001-20260804-002"));
            Assert.That(elsewhere, Is.EqualTo("EC-20260804-002-20260804-001"), "another case counts from its own start");
            Assert.That(this.issued.Series, Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task OneInstantIsTwoDaysInTwoZones()
    {
        var instant = new DateTime(2026, 8, 4, 22, 30, 0, DateTimeKind.Utc);

        this.time = new TestTimeProvider(instant, Prague);
        var east = await this.IssueCaseNumber(this.Issuer());

        this.time = new TestTimeProvider(instant, NewYork);
        var west = await this.IssueCaseNumber(this.Issuer());

        string[] expected = ["EC-20260805-", "EC-20260804-"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(east, Is.EqualTo("EC-20260805-001"), "half past midnight in Prague is already the fifth, whatever UTC still says");
            Assert.That(west, Is.EqualTo("EC-20260804-001"), "the same instant is still the fourth an afternoon west of it");
            Assert.That(this.issued.Series, Is.EqualTo(expected), "the series counts within a day of the zone the application runs in");
        }
    }

    /// <summary>
    /// One hour on either side of midnight in Prague, in January. The summer's two hours would put the
    /// earlier one on the fifth already, and UTC would leave the later one on the fourth.
    /// </summary>
    [Test]
    public async Task TheOffsetIsTheOneInForceOnTheDay()
    {
        this.time = new TestTimeProvider(new DateTime(2026, 1, 4, 22, 30, 0, DateTimeKind.Utc), Prague);
        var beforeMidnight = await this.IssueCaseNumber(this.Issuer());

        this.time = new TestTimeProvider(new DateTime(2026, 1, 4, 23, 30, 0, DateTimeKind.Utc), Prague);
        var afterMidnight = await this.IssueCaseNumber(this.Issuer());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(beforeMidnight, Is.EqualTo("EC-20260104-001"), "a zone keeps its own summer time rules, and January is not the summer's two hours");
            Assert.That(afterMidnight, Is.EqualTo("EC-20260105-001"), "an hour is still an hour in winter, and the zone is the one the application runs in rather than UTC");
        }
    }

    [Test]
    public async Task AnActOfAHandNumberedCaseIsWrittenUnderThatNumber()
    {
        var number = await this.IssueActNumber(this.Issuer(), caseId: 7);

        Assert.That(number, Is.EqualTo("OLD-2019/16-20260804-001"), "the case's own number is read from the case, whether it was issued here or typed in");
    }

    [Test]
    public void AnActOfACaseThatIsNotThereIsRefused()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(async () => await this.IssueActNumber(this.Issuer(), caseId: 404), Throws.InstanceOf<CaseNotFoundException>(), "the caller answers a missing case with a 404, so it must not arrive as the same failure a broken pattern does");
            Assert.That(this.issued.Series, Is.Empty, "a case no number can be written under is not a series to read either");
        }
    }

    [Test]
    public void APatternThatCannotWorkIsRefusedRatherThanIssuedFrom()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                async () => await this.IssueCaseNumber(this.Issuer(caseNumberPattern: "EC-{year}")),
                Throws.InstanceOf<InvalidOperationException>().And.Not.InstanceOf<CaseNotFoundException>(),
                "without {seq} every case would take the same number, which is not a case anybody is missing");
            Assert.That(async () => await this.IssueCaseNumber(this.Issuer(caseNumberPattern: "EC-{day}-{seq}")), Throws.InstanceOf<InvalidOperationException>(), "the fifth of September would take the numbers of the fifth of August");
            Assert.That(this.issued.Series, Is.Empty, "a pattern that cannot work is not a series to read either");
        }
    }

    /// <summary>
    /// What two callers reading one maximum do to each other, with the second's insert refused. The
    /// same thing against a server, and against callers that really are at once, is
    /// <c>NumberIssuerRaceTests</c>.
    /// </summary>
    [Test]
    public async Task ANumberTheColumnRefusesIsIssuedAgainOverWhatIsThereNow()
    {
        var attempts = new List<string>();

        var number = await this.Issuer().IssueCaseNumber((issuedNumber, _) =>
        {
            attempts.Add(issuedNumber);

            if (attempts.Count == 1)
            {
                // The caller that read the same maximum committed first; the row is there now.
                this.issued.KeepCaseNumber(issuedNumber);

                throw new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
            }

            return Task.FromResult(this.issued.KeepCaseNumber(issuedNumber));
        });

        string[] expected = ["EC-20260804-001", "EC-20260804-002"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(number, Is.EqualTo("EC-20260804-002"));
            Assert.That(attempts, Is.EqualTo(expected), "the create is run again under the number after the one that was taken");
        }
    }

    [Test]
    public void AColumnRefusingEveryNumberStopsRatherThanSpins()
    {
        var attempts = 0;

        Assert.That(
            async () => await this.Issuer().IssueCaseNumber<string>((_, _) =>
            {
                attempts++;

                throw new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
            }),
            Throws.InstanceOf<PostgresException>(),
            "a database refusing a number it does not already hold is not something to retry forever");

        Assert.That(attempts, Is.EqualTo(25), "the bound covers a burst of callers taking the numbers in front of one another");
    }

    [Test]
    public void AFailureThatIsNotATakenNumberIsNotRetried()
    {
        var attempts = 0;

        Assert.That(
            async () => await this.Issuer().IssueCaseNumber<string>((_, _) =>
            {
                attempts++;

                throw new InvalidOperationException("the create is broken");
            }),
            Throws.InstanceOf<InvalidOperationException>());

        Assert.That(attempts, Is.EqualTo(1), "only a number somebody else took is worth trying again");
    }

    private Task<string> IssueCaseNumber(NumberIssuer issuer) =>
        issuer.IssueCaseNumber((number, _) => Task.FromResult(this.issued.KeepCaseNumber(number)));

    private Task<string> IssueActNumber(NumberIssuer issuer, long caseId) =>
        issuer.IssueActNumber(caseId, (number, _) => Task.FromResult(this.issued.KeepActNumber(caseId, number)));

    private NumberIssuer Issuer(string? caseNumberPattern = null) =>
        new(new FakeNumberingSettingsReader(caseNumberPattern), this.issued, this.cases, this.time);
}
