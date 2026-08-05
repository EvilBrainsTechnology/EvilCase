using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The reader against the row the migration seeds, and against a database that has lost it.
/// </summary>
public class NumberingSettingsReaderTests
{
    private NumberingDatabase? database;

    private NumberingDatabase Database => this.database!;

    [SetUp]
    public async Task SetUp() => this.database = await NumberingDatabase.Create();

    // Without a server SetUp ignores the test and leaves nothing here to drop.
    [TearDown]
    public async Task TearDown()
    {
        if (this.database is not null)
            await this.database.DisposeAsync();
    }

    [Test]
    public async Task TheSeededRowIsWhatTheApplicationIssuesFrom()
    {
        var patterns = await this.Read();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patterns.CaseNumberPattern, Is.EqualTo(NumberingDefaults.CaseNumberPattern));
            Assert.That(patterns.ActNumberPattern, Is.EqualTo(NumberingDefaults.ActNumberPattern));
        }
    }

    [Test]
    public async Task WhatWasSavedWinsOverTheDefaults()
    {
        await this.Save("ACME-{year}-{seq}", "ACME-{case-number}/{seq}");

        var patterns = await this.Read();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patterns.CaseNumberPattern, Is.EqualTo("ACME-{year}-{seq}"));
            Assert.That(patterns.ActNumberPattern, Is.EqualTo("ACME-{case-number}/{seq}"), "the act's pattern is not the case's");
        }
    }

    [Test]
    public async Task ADatabaseWithoutTheRowFallsBackToTheDefaultsInTheirOwnPlaces()
    {
        await this.DeleteTheRow();

        var patterns = await this.Read();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patterns.CaseNumberPattern, Is.EqualTo(NumberingDefaults.CaseNumberPattern));
            Assert.That(patterns.ActNumberPattern, Is.EqualTo(NumberingDefaults.ActNumberPattern), "the act's default is not the case's");
        }
    }

    [Test]
    public async Task ASecondRowIsRefusedRatherThanChosenBetween()
    {
        await using (var context = this.Database.Context())
        {
            context.NumberingSettings.Add(new NumberingSettings { CaseNumberPattern = "X-{seq}", ActNumberPattern = "Y-{seq}" });
            await context.SaveChangesAsync();
        }

        Assert.That(async () => await this.Read(), Throws.InstanceOf<InvalidOperationException>(), "the settings are one row, so a second one is a broken database rather than a newer one to prefer");
    }

    private async Task<NumberingPatterns> Read()
    {
        await using var context = this.Database.Context();

        return await new NumberingSettingsReader(context).Read();
    }

    private async Task Save(string caseNumberPattern, string actNumberPattern)
    {
        await using var context = this.Database.Context();

        await context.NumberingSettings.ExecuteUpdateAsync(settings => settings
            .SetProperty(row => row.CaseNumberPattern, caseNumberPattern)
            .SetProperty(row => row.ActNumberPattern, actNumberPattern));
    }

    private async Task DeleteTheRow()
    {
        await using var context = this.Database.Context();

        await context.NumberingSettings.ExecuteDeleteAsync();
    }
}
