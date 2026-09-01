using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// What the next act number of a day inside a case reads, on the rows a real PostgreSQL returns. Each
/// test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ActNumberQueryTests : TenantFixture
{
    private static readonly DateOnly CaseDay = new(2026, 8, 7);

    private static readonly DateOnly ActDay = new(2026, 8, 12);

    private Case ownCase = null!;

    [SetUp]
    public async Task SetUpCase()
    {
        this.ownCase = await this.Tenant.AddCase(CaseDay);
    }

    [Test]
    public async Task ThePrefixNarrowsToTheCasesOwnNumbersOfTheDay()
    {
        await this.Tenant.AddAct(this.ownCase, ActDay);
        await this.Tenant.AddAct(this.ownCase, ActDay);
        await this.Tenant.AddAct(this.ownCase, new DateOnly(2026, 8, 13));
        await this.Tenant.AddAct(this.ownCase, ActDay, actNumber: $"{this.ownCase.CaseNumber}/2026-117");

        var otherCase = await this.Tenant.AddCase(CaseDay);
        await this.Tenant.AddAct(otherCase, ActDay);

        var numbers = await this.NumbersOfCaseWithPrefix(this.ownCase, ActNumberFormat.Prefix(this.ownCase.CaseNumber, ActDay));

        string[] expected =
        [
            ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 1),
            ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 2),
        ];

        Assert.That(
            numbers,
            Is.EquivalentTo(expected),
            "another day, another case and a hand-written value outside the format all drop out");
    }

    [Test]
    public async Task AWildcardInAHandWrittenCaseNumberMatchesOnlyItself()
    {
        var written = await this.Tenant.AddCase(CaseDay, caseNumber: @"EC/100%_1-a\b");

        await this.Tenant.AddAct(written, ActDay);

        // The same case, carrying a number only the unescaped pattern would reach.
        await this.Tenant.AddAct(written, ActDay, actNumber: "EC/100ZZZ_1-ab/20260812-777");

        var numbers = await this.NumbersOfCaseWithPrefix(written, ActNumberFormat.Prefix(written.CaseNumber, ActDay));

        string[] expected = [ActNumberFormat.Compose(written.CaseNumber, ActDay, 1)];

        Assert.That(numbers, Is.EqualTo(expected), "a wildcard in a hand-written case number matches only itself");
    }

    [Test]
    public async Task ANumberThatGrewADigitComesFirst()
    {
        await this.AddNumberedAct(999);
        await this.AddNumberedAct(1000);
        await this.AddNumberedAct(998);

        var numbers = await this.Tenant.Context.Acts
            .OfCaseWithNumberPrefix(this.ownCase.Id, ActNumberFormat.Prefix(this.ownCase.CaseNumber, ActDay))
            .OrderByNumberDescending()
            .Select(static act => act.ActNumber)
            .ToListAsync();

        string[] expected =
        [
            ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 1000),
            ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 999),
            ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 998),
        ];

        var next = ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, 1001);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                numbers,
                Is.EqualTo(expected),
                "a sequence that grew a digit outranks a three-digit one, and the step orders without taking a row");
            Assert.That(
                ActNumberFormat.Next(this.ownCase.CaseNumber, ActDay, numbers[0]),
                Is.EqualTo(next),
                "the issuer takes the row the order puts first");
        }
    }

    private async Task AddNumberedAct(int sequence)
    {
        await this.Tenant.AddAct(
            this.ownCase,
            ActDay,
            actNumber: ActNumberFormat.Compose(this.ownCase.CaseNumber, ActDay, sequence));
    }

    [Test]
    public async Task ANumberIsHeldOnlyByAnotherAct()
    {
        var first = await this.Tenant.AddAct(this.ownCase, ActDay);
        var second = await this.Tenant.AddAct(this.ownCase, ActDay);

        var byFirst = await this.Tenant.Context.Acts.WithNumberHeldByAnother(first.ActNumber, first.Id).ToListAsync();
        var bySecond = await this.Tenant.Context.Acts.WithNumberHeldByAnother(second.ActNumber, first.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byFirst, Is.Empty, "an act does not hold its own number against itself");
            Assert.That(bySecond.Select(static act => act.Id), Is.EqualTo([second.Id]));
        }
    }

    private async Task<List<string>> NumbersOfCaseWithPrefix(Case @case, string prefix)
    {
        return await this.Tenant.Context.Acts
            .OfCaseWithNumberPrefix(@case.Id, prefix)
            .Select(static act => act.ActNumber)
            .ToListAsync();
    }
}
