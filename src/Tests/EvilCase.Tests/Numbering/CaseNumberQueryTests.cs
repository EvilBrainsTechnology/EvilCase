using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// What the next case number of a day reads, on the rows a real PostgreSQL returns. Each test seeds a
/// tenant of its own, so none cleans up after itself.
/// </summary>
public class CaseNumberQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task ThePrefixNarrowsToTheDaysOwnNumbers()
    {
        await this.tenant.AddCase(Day);
        await this.tenant.AddCase(Day);
        await this.tenant.AddCase(new DateOnly(2026, 8, 8));
        await this.tenant.AddCase(Day, caseNumber: "2026/117-Ber");

        var numbers = await this.NumbersWithPrefix(CaseNumberFormat.Prefix(Day));

        string[] expected = [CaseNumberFormat.Compose(Day, 1), CaseNumberFormat.Compose(Day, 2)];

        Assert.That(
            numbers,
            Is.EquivalentTo(expected),
            "another day's number and a hand-written value outside the format both carry another prefix and drop out");
    }

    [Test]
    public async Task AWildcardInAHandWrittenCaseNumberMatchesOnlyItself()
    {
        await this.tenant.AddCase(Day, caseNumber: @"EC/100%_1-a\b");
        await this.tenant.AddCase(Day, caseNumber: "EC/100ZZZ_1-ab");
        await this.tenant.AddCase(Day, caseNumber: "EC/100%_1-aXb");

        var numbers = await this.NumbersWithPrefix(@"EC/100%_1-a\b");

        string[] expected = [@"EC/100%_1-a\b"];

        Assert.That(numbers, Is.EqualTo(expected), "a wildcard in a hand-written case number matches only itself");
    }

    [Test]
    public async Task ANumberThatGrewADigitComesFirst()
    {
        await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 999));
        await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 1000));
        await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 998));

        var numbers = await this.tenant.Context.Cases
            .WithNumberPrefix(CaseNumberFormat.Prefix(Day))
            .OrderByNumberDescending()
            .Select(@case => @case.CaseNumber)
            .ToListAsync();

        string[] expected = [CaseNumberFormat.Compose(Day, 1000), CaseNumberFormat.Compose(Day, 999), CaseNumberFormat.Compose(Day, 998)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(numbers, Is.EqualTo(expected), "a sequence that grew a digit outranks a three-digit one, and the step orders without taking a row");
            Assert.That(CaseNumberFormat.Next(Day, numbers[0]), Is.EqualTo(CaseNumberFormat.Compose(Day, 1001)), "the issuer takes the row the order puts first");
        }
    }

    [Test]
    public async Task ACaseOfAnotherTenantNeverComesBack()
    {
        await this.tenant.AddCase(Day);

        await using (var other = await TestTenant.Create())
        {
            await other.AddCase(Day);
            await other.AddCase(Day);
        }

        var numbers = await this.NumbersWithPrefix(CaseNumberFormat.Prefix(Day));

        string[] expected = [CaseNumberFormat.Compose(Day, 1)];

        Assert.That(numbers, Is.EqualTo(expected), "the tenant query filter is what keeps another tenant's numbers out of the day's highest");
    }

    [Test]
    public async Task ANumberIsHeldOnlyByAnotherCase()
    {
        var first = await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 1));
        var second = await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 2));

        var byFirst = await this.tenant.Context.Cases.WithNumberHeldByAnother(first.CaseNumber, first.Id).ToListAsync();
        var bySecond = await this.tenant.Context.Cases.WithNumberHeldByAnother(second.CaseNumber, first.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byFirst, Is.Empty, "a case does not hold its own number against itself");
            Assert.That(bySecond.Select(@case => @case.Id), Is.EqualTo([second.Id]));
        }
    }

    private async Task<List<string>> NumbersWithPrefix(string prefix)
    {
        return await this.tenant.Context.Cases
            .WithNumberPrefix(prefix)
            .Select(@case => @case.CaseNumber)
            .ToListAsync();
    }
}
