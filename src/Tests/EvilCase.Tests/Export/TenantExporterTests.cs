using System.IO.Compression;
using System.Text;
using System.Text.Json;
using EvilBrains.EvilCase.Business.Export;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Labels;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Files;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NJsonSchema;

namespace EvilBrains.EvilCase.Tests.Export;

public class TenantExporterTests : TenantFixture
{
    private const string Root = "cases/2026-08-21 Přestupek";

    private static readonly DateOnly Day = new(2026, 8, 21);

    private static readonly string[] EmptyTenantEntries = ["export.json", "cases/", "contacts/", "labels/"];

    private FakeFileBlobStore blobs = null!;

    private TenantExporter exporter = null!;

    [SetUp]
    public void SetUpExporter()
    {
        this.blobs = new FakeFileBlobStore();
        this.exporter = new TenantExporter(
            new TransactionalDbSession(this.Tenant.Context),
            this.Tenant.UserContext,
            this.blobs,
            TimeProvider.System,
            NullLogger<TenantExporter>.Instance);
    }

    [Test]
    public async Task CasesNestAsFoldersWithTheirActsFilesAndVersions()
    {
        var tree = await this.SeedTree();

        var entries = await this.Export();

        var appeal = $"{Root}/cases/2026-09-01 Odvolání";
        var cassation = $"{appeal}/cases/2026-10-01 Kasační stížnost";
        var filing = $"{cassation}/acts/2026-10-02 Podání";

        string[] expected =
        [
            .. EmptyTenantEntries,
            $"{Root}/case.json",
            $"{Root}/files/Smlouva (verze 1).pdf",
            $"{Root}/files/Smlouva.pdf",
            $"{appeal}/case.json",
            $"{cassation}/case.json",
            $"{filing}/act.json",
            $"{filing}/files/Podání.pdf",
            "contacts/Krajský úřad.json",
            "labels/labels.json",
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Keys, Is.EquivalentTo(expected));
            Assert.That(entries[$"{Root}/files/Smlouva.pdf"], Is.EqualTo("smlouva 2"), "the current version goes under the file's name");
            Assert.That(entries[$"{Root}/files/Smlouva (verze 1).pdf"], Is.EqualTo("smlouva 1"));
        }

        var root = Document(entries, $"{Root}/case.json");
        var versions = root.GetProperty("files")[0].GetProperty("versions");
        var cassationCase = Document(entries, $"{cassation}/case.json");
        var act = Document(entries, $"{filing}/act.json");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.GetProperty("parentCaseId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(root.GetProperty("contact").GetProperty("name").GetString(), Is.EqualTo("Krajský úřad"));
            Assert.That(root.GetProperty("labels")[0].GetProperty("name").GetString(), Is.EqualTo("InfZ"));
            Assert.That(root.GetProperty("comments")[0].GetProperty("authorEmail").GetString(), Is.EqualTo(tree.UserEmail));
            Assert.That(versions.EnumerateArray().Select(static version => version.GetProperty("path").GetString()), Is.EqualTo([$"{Root}/files/Smlouva (verze 1).pdf", $"{Root}/files/Smlouva.pdf"]));
            Assert.That(cassationCase.GetProperty("parentCaseId").GetGuid(), Is.EqualTo(tree.AppealId), "a sub-case names the case above it");
            Assert.That(act.GetProperty("caseId").GetGuid(), Is.EqualTo(tree.CassationId));
            Assert.That(act.GetProperty("direction").GetString(), Is.EqualTo("Outgoing"));
        }
    }

    [Test]
    public async Task EveryDocumentValidatesAgainstTheSchemaOfItsType()
    {
        await this.SeedTree();

        var entries = await this.Export();
        var documents = entries.Where(static entry => entry.Key.EndsWith(".json", StringComparison.Ordinal)).ToList();
        var schemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);

        foreach (var type in new[] { "export", "case", "act", "contact", "labels" })
            schemas[type] = await JsonSchema.FromFileAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "ExportSchemas", "v1", $"{type}.schema.json"));

        Assert.That(documents.Select(static document => Document(document.Value).GetProperty("type").GetString()).Distinct(), Is.EquivalentTo(schemas.Keys), "the tree holds a document of every type");

        using (Assert.EnterMultipleScope())
        {
            foreach (var (path, json) in documents)
            {
                var errors = schemas[Document(json).GetProperty("type").GetString()!].Validate(json);

                Assert.That(errors.Select(static error => error.ToString()), Is.Empty, $"{path} follows the schema of its type");
            }
        }
    }

    [Test]
    public async Task ANameTakenInItsFolderGetsANumberAndTheDocumentKeepsTheRealOne()
    {
        var first = await this.Tenant.AddCase(Day, title: "Přestupek");
        var second = await this.Tenant.AddCase(Day, title: "Přestupek");
        await this.Tenant.AddCase(Day, title: "Podání", parentCaseId: first.Id);
        await this.Tenant.AddAct(first, Day, title: "Podání");
        await this.Tenant.AddAct(first, Day, title: "Podání");
        this.Stored(await this.Tenant.AddCaseFile(first, "case.json"), "soubor");
        this.Stored(await this.Tenant.AddCaseFile(first, "Smlouva?.pdf"), "smlouva a");
        this.Stored(await this.Tenant.AddCaseFile(first, "Smlouva*.pdf"), "smlouva b");
        await this.Tenant.AddContact("Novák");
        await this.Tenant.AddContact("Novák");

        var entries = await this.Export();

        var secondCase = Document(entries, $"{Root} (2)/case.json");
        var files = Document(entries, $"{Root}/case.json").GetProperty("files").EnumerateArray().ToList();

        string[] numbered =
        [
            $"{Root}/case.json",
            $"{Root}/files/case.json",
            $"{Root}/files/Smlouva_.pdf",
            $"{Root}/files/Smlouva_ (2).pdf",
            $"{Root}/cases/2026-08-21 Podání/case.json",
            $"{Root}/acts/2026-08-21 Podání/act.json",
            $"{Root}/acts/2026-08-21 Podání (2)/act.json",
            "contacts/Novák.json",
            "contacts/Novák (2).json",
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Keys, Is.SupersetOf(numbered), "documents, files, sub-cases and acts each claim names in their own folder");
            Assert.That(secondCase.GetProperty("caseId").GetGuid(), Is.EqualTo(second.Id));
            Assert.That(secondCase.GetProperty("title").GetString(), Is.EqualTo("Přestupek"));
            Assert.That(secondCase.GetProperty("path").GetString(), Is.EqualTo($"{Root} (2)"));
            Assert.That(entries[$"{Root}/files/case.json"], Is.EqualTo("soubor"));
            Assert.That(files.Select(static file => file.GetProperty("fileName").GetString()), Is.EqualTo(["case.json", "Smlouva?.pdf", "Smlouva*.pdf"]), "a numbered file keeps its real name in the document");
        }
    }

    [Test]
    public async Task AnEmptyTenantExportsTheManifestAndEmptyFolders()
    {
        var entries = await this.Export();
        var manifest = Document(entries, "export.json");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Keys, Is.EquivalentTo(EmptyTenantEntries));
            Assert.That(manifest.GetProperty("formatVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(manifest.GetProperty("tenantId").GetGuid(), Is.EqualTo(this.Tenant.UserContext.TenantId));
            Assert.That(manifest.GetProperty("tenantName").GetString(), Is.EqualTo("tenant"));
        }
    }

    [Test]
    public async Task AVersionWhoseBlobIsMissingIsMarkedAndTheExportCompletes()
    {
        var @case = await this.Tenant.AddCase(Day, title: "Přestupek");
        var file = this.Stored(await this.Tenant.AddCaseFile(@case, "Smlouva.pdf"), "smlouva 1");
        await this.Tenant.AddFileVersion(file);

        var entries = await this.Export();
        var versions = Document(entries, $"{Root}/case.json").GetProperty("files")[0].GetProperty("versions");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Keys, Does.Contain($"{Root}/files/Smlouva (verze 1).pdf"));
            Assert.That(entries.Keys, Does.Not.Contain($"{Root}/files/Smlouva.pdf"), "a missing blob has no entry");
            Assert.That(versions[1].GetProperty("missing").GetBoolean(), Is.True);
            Assert.That(versions[1].GetProperty("path").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(versions[0].GetProperty("missing").GetBoolean(), Is.False);
        }
    }

    [Test]
    public async Task TheExportHoldsOnlyTheCallersTenant()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, title: "Cizí spis");
        await other.AddAct(otherCase, Day, title: "Cizí úkon");
        await other.AddContact("Cizí kontakt");
        await other.AddLabel("Cizí štítek");
        await this.Tenant.AddCase(Day, title: "Přestupek");

        var entries = await this.Export();

        Assert.That(entries.Keys, Is.EquivalentTo([.. EmptyTenantEntries, $"{Root}/case.json"]), "never another tenant's case, act, contact or label");
    }

    private static JsonElement Document(Dictionary<string, string> entries, string path)
    {
        return Document(entries[path]);
    }

    private static JsonElement Document(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// A root case with a contact, a label, a comment and a file in two versions; a sub-case under it, a sub-case under
    /// that one, and an act there with a file.
    /// </summary>
    private async Task<SeededTree> SeedTree()
    {
        var contact = await this.Tenant.AddContact("Krajský úřad", dataBoxId: "abc1234", address: "Náměstí 1, Praha");
        var label = await this.Tenant.AddLabel("InfZ", LabelColor.Teal);

        var root = await this.Tenant.AddCase(Day, title: "Přestupek", description: "Popis", externalCaseNumber: "KUP 1/2026", contact: contact);
        await this.Tenant.AddCaseLabel(root, label);
        await this.Tenant.AddCaseComment(root, "Poznámka");
        var smlouva = this.Stored(await this.Tenant.AddCaseFile(root, "Smlouva.pdf"), "smlouva 1");
        this.Stored(await this.Tenant.AddFileVersion(smlouva), "smlouva 2");

        var appeal = await this.Tenant.AddCase(new DateOnly(2026, 9, 1), title: "Odvolání", parentCaseId: root.Id);
        var cassation = await this.Tenant.AddCase(new DateOnly(2026, 10, 1), title: "Kasační stížnost", parentCaseId: appeal.Id);
        var act = await this.Tenant.AddAct(cassation, new DateOnly(2026, 10, 2), title: "Podání", contact: contact, direction: ActDirection.Outgoing);
        await this.Tenant.AddActLabel(act, label);
        await this.Tenant.AddActComment(act, "Odesláno");
        this.Stored(await this.Tenant.AddActFile(act, "Podání.pdf"), "podani");

        var userEmail = await this.Tenant.Context.Users
            .Where(user => user.Id == this.Tenant.UserId)
            .Select(static user => user.Email)
            .SingleAsync();

        return new SeededTree(appeal.Id, cassation.Id, userEmail);
    }

    private FileAsset Stored(FileAsset file, string content)
    {
        this.blobs.WrittenByPath[file.Versions.Single().StoragePath] = content;

        return file;
    }

    private void Stored(FileVersion version, string content)
    {
        this.blobs.WrittenByPath[version.StoragePath] = content;
    }

    /// <summary>
    /// Each entry's name to its text.
    /// </summary>
    private async Task<Dictionary<string, string>> Export()
    {
        var target = new AsyncOnlyStream();
        await this.exporter.WriteTenantExport(this.exporter.CreateTenantExport(), target, CancellationToken.None);

        await using var zip = new ZipArchive(new MemoryStream(target.Written()));

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in zip.Entries)
        {
            await using var content = await entry.OpenAsync();
            using var text = new StreamReader(content, Encoding.UTF8);

            entries[entry.FullName] = await text.ReadToEndAsync();
        }

        return entries;
    }

    private sealed record SeededTree(Guid AppealId, Guid CassationId, string UserEmail);
}
