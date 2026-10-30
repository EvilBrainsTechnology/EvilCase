using System.IO.Compression;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// Holds the outline of all cases and acts at once and reads the rest one case or act at a time.
/// </summary>
internal sealed class TenantExporter(
    IDbSession dbSession,
    IUserContext userContext,
    IFileBlobStore blobStore,
    TimeProvider timeProvider,
    ILogger<TenantExporter> logger) : ITenantExporter
{
    // Czech text stays readable; nothing renders these documents as HTML.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly string ApplicationVersion = typeof(TenantExporter).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion;

    public TenantExport CreateTenantExport()
    {
        var started = timeProvider.GetUtcNow();

        return new TenantExport { FileName = TenantExportLayout.ArchiveName(started), Started = started };
    }

    public async Task WriteTenantExport(TenantExport export, Stream target, CancellationToken token)
    {
        var context = dbSession.Current;
        var tenantId = userContext.TenantId;

        logger.LogInformation("Export of tenant {TenantId} started", tenantId);

        await using var snapshot = await dbSession.BeginSnapshot(token);

        var tenantName = await context.Tenants
            .WithId(tenantId)
            .Select(static tenant => tenant.Name)
            .SingleAsync(token);

        var cases = await context.Cases.AsOutlinedCases().ToListAsync(token);
        var acts = await context.Acts.AsOutlinedActs().ToListAsync(token);
        var contacts = await context.Contacts.AsExportedContacts().ToListAsync(token);
        var labels = await context.Labels.AsExportedLabels().ToListAsync(token);
        var versionCount = await context.FileVersions.CountAsync(token);

        var started = TimeZoneInfo.ConvertTime(export.Started, FileArchiveLayout.Prague);

        await using var zip = await ZipStreamWriter.Create(target, token);

        var manifest = new ExportManifest
        {
            FormatVersion = TenantExportLayout.FormatVersion,
            Exported = export.Started.UtcDateTime,
            ApplicationVersion = ApplicationVersion,
            TenantId = tenantId,
            TenantName = tenantName,
        };

        await WriteJson(zip, TenantExportLayout.ManifestName, started, manifest, token);
        await zip.WriteFolder(TenantExportLayout.CasesFolder, started, token);
        await zip.WriteFolder(TenantExportLayout.ContactsFolder, started, token);
        await zip.WriteFolder(TenantExportLayout.LabelsFolder, started, token);

        var missing = await this.WriteCases(zip, cases, acts, token);

        await WriteContacts(zip, contacts, token);

        if (labels.Count > 0)
        {
            var labelList = new ExportedLabelList { Labels = labels };

            await WriteJson(zip, $"{TenantExportLayout.LabelsFolder}/{TenantExportLayout.LabelsDocumentName}", started, labelList, token);
        }

        await zip.Complete(token);

        logger.LogInformation(
            "Export of tenant {TenantId} finished: {CaseCount} cases, {ActCount} acts, {ContactCount} contacts, {LabelCount} labels, {FileVersionCount} file versions of which {MissingBlobCount} missing, {ByteCount} bytes",
            tenantId,
            cases.Count,
            acts.Count,
            contacts.Count,
            labels.Count,
            versionCount,
            missing,
            zip.BytesWritten);
    }

    private static async Task WriteJson<TDocument>(ZipStreamWriter zip, string entryName, DateTimeOffset lastWriteTime, TDocument document, CancellationToken token)
    {
        await zip.WriteEntry(entryName, lastWriteTime, CompressionLevel.Optimal, async entryStream => await JsonSerializer.SerializeAsync(entryStream, document, Json, token), token);
    }

    private static async Task WriteContacts(ZipStreamWriter zip, List<ExportedContact> contacts, CancellationToken token)
    {
        var names = FileArchiveLayout.ClaimedNames();

        foreach (var contact in contacts)
        {
            var path = $"{TenantExportLayout.ContactsFolder}/{FileArchiveLayout.ClaimFileName(names, TenantExportLayout.ContactDocumentName(contact.Name))}";

            await WriteJson(zip, path, FileArchiveLayout.PragueTime(contact.Updated ?? contact.Created), contact with { Path = path }, token);
        }
    }

    private static List<ExportedFile> ExportedFiles(List<ExportedVersionRow> rows, Dictionary<Guid, string?> entryPaths)
    {
        return
        [
            .. rows
                .GroupBy(static row => row.FileId)
                .Select(file => new ExportedFile
                {
                    FileId = file.Key,
                    FileName = file.First().FileName,
                    Versions =
                    [
                        .. file.Select(row => new ExportedFileVersion
                        {
                            Number = row.Number,
                            SizeBytes = row.SizeBytes,
                            MediaType = row.MediaType,
                            Sha256 = row.ContentHash,
                            Created = row.Created,
                            UploaderEmail = row.UploaderEmail,
                            Path = entryPaths[row.FileVersionId],
                            Missing = entryPaths[row.FileVersionId] is null,
                        }),
                    ],
                }),
        ];
    }

    /// <summary>
    /// The root cases in Spisy, each with everything below it; answers how many blobs were missing.
    /// </summary>
    private async Task<int> WriteCases(ZipStreamWriter zip, List<OutlinedCase> cases, List<OutlinedAct> acts, CancellationToken token)
    {
        var subCases = cases.ToLookup(static @case => @case.ParentCaseId);
        var caseActs = acts.ToLookup(static act => act.CaseId);
        var names = FileArchiveLayout.ClaimedNames();
        var missing = 0;

        foreach (var root in subCases[null])
        {
            var name = FileArchiveLayout.ClaimFolderName(names, TenantExportLayout.FolderName(root.Date, root.Title, TenantExportLayout.CaseFallback));

            missing += await this.WriteCaseFolder(zip, subCases, caseActs, root, $"{TenantExportLayout.CasesFolder}/{name}", token);
        }

        return missing;
    }

    /// <summary>
    /// The case's document beside its files, then its sub-cases and acts as folders; answers how many blobs were missing.
    /// </summary>
    private async Task<int> WriteCaseFolder(
        ZipStreamWriter zip,
        ILookup<Guid?, OutlinedCase> subCases,
        ILookup<Guid, OutlinedAct> caseActs,
        OutlinedCase @case,
        string path,
        CancellationToken token)
    {
        var context = dbSession.Current;
        var names = FileArchiveLayout.ClaimedNames();
        names.Add(TenantExportLayout.CaseDocumentName);

        var children = subCases[@case.CaseId]
            .Select(child => (Case: child, Name: FileArchiveLayout.ClaimFolderName(names, TenantExportLayout.FolderName(child.Date, child.Title, TenantExportLayout.CaseFallback))))
            .ToList();

        var acts = caseActs[@case.CaseId]
            .Select(act => (Act: act, Name: FileArchiveLayout.ClaimFolderName(names, TenantExportLayout.FolderName(act.Date, act.Title, TenantExportLayout.ActFallback))))
            .ToList();

        var (files, missing) = await this.WriteFiles(zip, context.FileVersions.OfCaseFiles(@case.CaseId), path, names, token);

        var comments = await context.Comments
            .OnCase(@case.CaseId)
            .AsExportedComments(context.Users)
            .InDiaryOrder()
            .ToListAsync(token);

        var exported = await context.Cases.ReadExportedCase(@case.CaseId, token);

        await WriteJson(
            zip,
            $"{path}/{TenantExportLayout.CaseDocumentName}",
            FileArchiveLayout.PragueTime(exported.Updated ?? exported.Created),
            exported with { Comments = comments, Files = files, Path = path },
            token);

        foreach (var child in children)
            missing += await this.WriteCaseFolder(zip, subCases, caseActs, child.Case, $"{path}/{child.Name}", token);

        foreach (var act in acts)
            missing += await this.WriteActFolder(zip, act.Act, $"{path}/{act.Name}", token);

        return missing;
    }

    private async Task<int> WriteActFolder(ZipStreamWriter zip, OutlinedAct act, string path, CancellationToken token)
    {
        var context = dbSession.Current;
        var names = FileArchiveLayout.ClaimedNames();
        names.Add(TenantExportLayout.ActDocumentName);

        var (files, missing) = await this.WriteFiles(zip, context.FileVersions.OfActFiles(act.ActId), path, names, token);

        var comments = await context.Comments
            .OnAct(act.ActId)
            .AsExportedComments(context.Users)
            .InDiaryOrder()
            .ToListAsync(token);

        var exported = await context.Acts.ReadExportedAct(act.ActId, token);

        await WriteJson(
            zip,
            $"{path}/{TenantExportLayout.ActDocumentName}",
            FileArchiveLayout.PragueTime(exported.Updated ?? exported.Created),
            exported with { Comments = comments, Files = files, Path = path },
            token);

        return missing;
    }

    /// <summary>
    /// Every version under the names "Stáhnout vše včetně verzí" gives them, against the names the folder holds already.
    /// A version whose blob is missing stays out of the archive and is marked in its file.
    /// </summary>
    private async Task<(List<ExportedFile> Files, int Missing)> WriteFiles(
        ZipStreamWriter zip,
        IQueryable<FileVersion> versions,
        string path,
        HashSet<string> names,
        CancellationToken token)
    {
        var rows = await versions
            .InFileOrder()
            .AsExportedVersionRows(dbSession.Current.Users)
            .ToListAsync(token);

        var archived = rows.ConvertAll(static row => new ArchivedVersion
        {
            FileId = row.FileId,
            FileName = row.FileName,
            Number = row.Number,
            Created = row.Created,
            StoragePath = row.StoragePath,
        });

        var entries = FileArchiveLayout
            .Entries(archived, includeVersions: true, names)
            .ToDictionary(static entry => entry.StoragePath, StringComparer.Ordinal);

        var entryPaths = new Dictionary<Guid, string?>();

        foreach (var row in rows)
        {
            var entry = entries[row.StoragePath];
            var entryPath = $"{path}/{entry.EntryName}";

            await using var content = blobStore.ReadFileBlob(row.StoragePath);

            if (content is null)
            {
                logger.LogWarning("The blob of file version {FileVersionId} is missing; the export marks the version missing", row.FileVersionId);
                entryPaths[row.FileVersionId] = null;
                continue;
            }

            // Stored documents are mostly compressed already; the fastest level still shrinks text and XML.
            await zip.WriteEntry(entryPath, entry.Created, CompressionLevel.Fastest, async entryStream => await content.CopyToAsync(entryStream, token), token);
            entryPaths[row.FileVersionId] = entryPath;
        }

        return (ExportedFiles(rows, entryPaths), entryPaths.Values.Count(static entryPath => entryPath is null));
    }
}
