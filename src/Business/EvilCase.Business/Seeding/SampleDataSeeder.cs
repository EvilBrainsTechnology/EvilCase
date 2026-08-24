using System.Text;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Files;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed class SampleDataSeeder(
    IDbSession dbSession,
    ICaseNumberIssuer caseNumberIssuer,
    IActNumberIssuer actNumberIssuer,
    IFileBlobStore fileBlobStore,
    IUserContext userContext,
    ILogger<SampleDataSeeder> logger) : ISampleDataSeeder
{
    public async Task SeedSampleData(Guid userId, CancellationToken token)
    {
        var user = await dbSession.Current.Users.FindAsync([userId], token)
            ?? throw new InvalidOperationException($"User {userId} does not exist.");
        var tenantId = user.TenantId;

        logger.LogInformation("Sample data seed started for tenant {TenantId}", tenantId);

        using var userScope = userContext.Enter(tenantId, userId);
        await using var transaction = await dbSession.BeginTransaction(token);

        var contactsByKey = await this.SeedContacts(token);
        var casesByKey = new Dictionary<string, Case>();
        var counters = new SeedCounters();

        foreach (var sampleCase in SampleData.Cases)
            await this.SeedCase(tenantId, sampleCase, contactsByKey, casesByKey, counters, token);

        await transaction.CommitAsync(token);

        logger.LogInformation(
            "Sample data seeded into tenant {TenantId}: {ContactCount} contacts, {CaseCount} cases, {ActCount} acts, "
                + "{ExternalNumberCount} external numbers, {CommentCount} comments, {FileCount} files",
            tenantId,
            contactsByKey.Count,
            casesByKey.Count,
            counters.ActCount,
            counters.ExternalNumberCount,
            counters.CommentCount,
            counters.FileCount);
    }

    private async Task<Dictionary<string, Contact>> SeedContacts(CancellationToken token)
    {
        var contactsByKey = new Dictionary<string, Contact>();

        foreach (var sampleContact in SampleData.Contacts)
        {
            var contact = new Contact
            {
                Kind = sampleContact.Kind,
                Name = sampleContact.Name,
                Address = sampleContact.Address,
                DataBoxId = sampleContact.DataBoxId,
            };

            contactsByKey[sampleContact.Key] = contact;
            dbSession.Current.Contacts.Add(contact);
        }

        await dbSession.Current.SaveChangesAsync(token);

        return contactsByKey;
    }

    private async Task SeedCase(
        Guid tenantId,
        SampleCase sampleCase,
        Dictionary<string, Contact> contactsByKey,
        Dictionary<string, Case> casesByKey,
        SeedCounters counters,
        CancellationToken token)
    {
        var caseNumber = await caseNumberIssuer.NextCaseNumber(sampleCase.Date, token);

        var @case = new Case
        {
            ParentCaseId = sampleCase.ParentKey is null ? null : casesByKey[sampleCase.ParentKey].Id,
            CaseNumber = caseNumber,
            Date = sampleCase.Date,
            Title = sampleCase.Title,
            Description = sampleCase.Description,
            Status = sampleCase.Status,
        };

        casesByKey[sampleCase.Key] = @case;
        dbSession.Current.Cases.Add(@case);
        await dbSession.Current.SaveChangesAsync(token);

        foreach (var externalNumber in sampleCase.ExternalNumbers)
        {
            dbSession.Current.ExternalCaseNumbers.Add(new ExternalCaseNumber
            {
                CaseId = @case.Id,
                Value = externalNumber.Value,
                AssignedByContactId = contactsByKey[externalNumber.AssignedByKey].Id,
            });

            counters.ExternalNumberCount++;
        }

        foreach (var body in sampleCase.Comments)
        {
            dbSession.Current.Comments.Add(new Comment { CaseId = @case.Id, Body = body });
            counters.CommentCount++;
        }

        if (string.Equals(sampleCase.Key, SampleData.MainCaseKey, StringComparison.Ordinal))
        {
            await this.AddFile(
                tenantId,
                @case.Id,
                actId: null,
                caseNumber.Replace('/', '-') + ".txt",
                FileBody(sampleCase.Title, "Spisová značka", caseNumber, sampleCase.Date),
                token);

            counters.FileCount++;
        }

        await dbSession.Current.SaveChangesAsync(token);

        var sampleActs = string.Equals(sampleCase.Key, SampleData.MainCaseKey, StringComparison.Ordinal)
            ? SampleData.MainCaseActs
            : SubCaseActs(sampleCase);

        foreach (var sampleAct in sampleActs)
            await this.SeedAct(tenantId, @case, sampleAct, contactsByKey, counters, token);
    }

    private async Task SeedAct(
        Guid tenantId,
        Case @case,
        SampleAct sampleAct,
        Dictionary<string, Contact> contactsByKey,
        SeedCounters counters,
        CancellationToken token)
    {
        var actNumber = await actNumberIssuer.NextActNumber(@case, sampleAct.Date, token);

        var counterparty = contactsByKey[sampleAct.CounterpartyKey];
        var subject = contactsByKey[SampleData.SubjectKey];
        var incoming = sampleAct.Direction == ActDirection.Incoming;

        var act = new Act
        {
            CaseId = @case.Id,
            ActNumber = actNumber,
            Direction = sampleAct.Direction,
            Title = sampleAct.Title,
            Description = sampleAct.Description,
            Date = sampleAct.Date,
            IssuedByContactId = incoming ? counterparty.Id : subject.Id,
            AddressedToContactId = incoming ? subject.Id : counterparty.Id,
        };

        dbSession.Current.Acts.Add(act);
        await dbSession.Current.SaveChangesAsync(token);
        counters.ActCount++;

        foreach (var externalNumber in sampleAct.ExternalNumbers)
        {
            dbSession.Current.ExternalActNumbers.Add(new ExternalActNumber
            {
                ActId = act.Id,
                Value = externalNumber.Value,
                AssignedByContactId = contactsByKey[externalNumber.AssignedByKey].Id,
            });

            counters.ExternalNumberCount++;
        }

        foreach (var body in sampleAct.Comments)
        {
            dbSession.Current.Comments.Add(new Comment { ActId = act.Id, Body = body });
            counters.CommentCount++;
        }

        await this.AddFile(tenantId, caseId: null, act.Id, actNumber.Replace('/', '-') + ".txt", FileBody(sampleAct.Title, "Číslo jednací", actNumber, sampleAct.Date), token);
        counters.FileCount++;

        if (sampleAct.ExtraFileSuffix is not null)
        {
            var fileName = actNumber.Replace('/', '-') + "-" + sampleAct.ExtraFileSuffix + ".txt";

            await this.AddFile(tenantId, caseId: null, act.Id, fileName, FileBody(sampleAct.Title, "Číslo jednací", actNumber, sampleAct.Date), token);
            counters.FileCount++;
        }

        await dbSession.Current.SaveChangesAsync(token);
    }

    /// <summary>
    /// The two generated acts every sub-case gets: the source only records their counts (SDD-017).
    /// </summary>
    private static IReadOnlyList<SampleAct> SubCaseActs(SampleCase sampleCase)
    {
        var counterpartyKey = sampleCase.CounterpartyKey ?? throw new InvalidOperationException($"Sub-case '{sampleCase.Key}' names no counterparty.");

        return
        [
            new SampleAct
            {
                Direction = ActDirection.Outgoing,
                Title = "Podání",
                Description = "Syntetické vzorové podání.",
                Date = sampleCase.Date,
                CounterpartyKey = counterpartyKey,
            },
            new SampleAct
            {
                Direction = ActDirection.Incoming,
                Title = "Odpověď",
                Description = "Syntetická vzorová odpověď.",
                Date = sampleCase.Date.AddDays(14),
                CounterpartyKey = counterpartyKey,
            },
        ];
    }

    private async Task AddFile(Guid tenantId, Guid? caseId, Guid? actId, string fileName, string content, CancellationToken token)
    {
        var fileAssetId = Guid.CreateVersion7();

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var blob = await fileBlobStore.WriteFileBlob(tenantId, fileAssetId, stream, token);

        dbSession.Current.FileAssets.Add(new FileAsset
        {
            Id = fileAssetId,
            CaseId = caseId,
            ActId = actId,
            FileName = fileName,
            ContentHash = blob.ContentHash,
            SizeBytes = blob.SizeBytes,
            MediaType = "text/plain",
            StoragePath = blob.StoragePath,
        });
    }

    private static string FileBody(string title, string numberLabel, string number, in DateOnly date)
    {
        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"""
            {title}
            {numberLabel}: {number}
            Datum: {dateText}

            Syntetický vzorový dokument. Neobsahuje žádný skutečný obsah spisu.
            """;
    }

    /// <summary>
    /// Running totals across the whole seed, threaded through the per-case and per-act helpers so the
    /// final log line reports what was actually written.
    /// </summary>
    private sealed class SeedCounters
    {
        public int ActCount { get; set; }

        public int ExternalNumberCount { get; set; }

        public int CommentCount { get; set; }

        public int FileCount { get; set; }
    }
}
