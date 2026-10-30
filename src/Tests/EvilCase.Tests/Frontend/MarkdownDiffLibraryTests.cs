using System.Text.Json;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MarkdownDiffLibraryTests
{
    // Prints the differences of two documents given as blocks: [kind, text or rows, { marks, in }] with kind p, h1 to h6,
    // code or table, marks as [from, to, marks] and in naming the containers outside in: quote, bullet, ordered, task,
    // done. A line names each mark, each deletion one column shows and the summary.
    private const string Script = """
        const [{ diffMarkdown }, { diffTexts }] = await Promise.all([import(process.argv[1]), import(process.argv[2])]);
        const specs = JSON.parse(process.argv[3]);

        function documentOf(blocks) {
            let pos = 0;
            const names = new Map();
            const texts = [];
            const textBlock = (type, level, value, marks, chain) => {
                const runs = [];
                let at = 0;
                for (const [from, to, set] of marks ?? []) {
                    if (from > at) runs.push([at, from, {}]);
                    runs.push([from, to, set]);
                    at = to;
                }
                if (value.length > at) runs.push([at, value.length, {}]);
                const block = { type, level, node: pos, pos: pos + 1, end: pos + value.length + 2, chain, text: value, runs };
                names.set(block.node, '"' + value + '"');
                texts.push(block);
                pos = block.end;
                return block;
            };
            const laid = blocks.map(([kind, value, extra]) => {
                const chain = [];
                for (const container of extra?.in ?? []) {
                    if (container === 'quote') {
                        chain.push({ type: 'blockquote', pos: pos++ });
                    } else {
                        chain.push({ type: container === 'ordered' ? 'ordered_list' : 'bullet_list', pos: pos++ });
                        chain.push({ type: 'list_item', kind: container, pos: pos++ });
                    }
                }
                let block;
                if (kind === 'table') {
                    const node = pos++;
                    const rows = value.map((cells) => {
                        const row = { node: pos++ };
                        row.cells = cells.map((cell) => {
                            const start = pos++;
                            const inner = textBlock('paragraph', 0, cell, null, []);
                            names.set(start, 'cell "' + cell + '"');
                            return { node: start, end: ++pos, block: inner };
                        });
                        row.end = ++pos;
                        names.set(row.node, 'row "' + cells[0] + '"');
                        names.set(row.end - 1, 'the end of row "' + cells[0] + '"');
                        return row;
                    });
                    block = { type: 'table', node, end: ++pos, chain, rows };
                    names.set(node, 'table');
                    names.set(block.end - 1, 'the end of the table');
                } else {
                    const type = kind === 'p' ? 'paragraph' : kind === 'code' ? 'code_block' : 'heading';
                    block = textBlock(type, kind[0] === 'h' ? Number(kind[1]) : 0, value, extra?.marks, chain);
                }
                for (const entry of chain) names.set(entry.pos, entry.type + ' of ' + (names.get(block.node) ?? 'table'));
                pos += chain.length;
                return block;
            });
            names.set(pos, 'the end');
            return { document: { size: pos, blocks: laid }, names, texts };
        }

        const [older, newer] = specs.map(documentOf);
        const result = diffMarkdown(older.document, newer.document, diffTexts);
        const slice = (side, from, to) => {
            const block = side.texts.find((each) => each.pos <= from && to <= each.end - 1);
            return '"' + block.text.slice(from - block.pos, to - block.pos) + '"';
        };
        const lines = [];
        for (const [name, side, marks] of [['older', older, result.older], ['newer', newer, result.newer]]) {
            for (const mark of marks) {
                const what = mark.from !== undefined ? slice(side, mark.from, mark.to) : side.names.get(mark.node);
                lines.push(name + ' ' + mark.type + ' ' + what + (mark.title ? ': ' + mark.title : ''));
            }
        }
        for (const deletion of result.deletions) {
            if (deletion.from !== undefined) {
                lines.push('deletion ' + slice(older, deletion.from, deletion.to) + ' before ' + slice(newer, deletion.at, deletion.at + 1));
            } else {
                const wrap = deletion.wrap.map((pos) => older.names.get(pos).split(' ')[0]);
                lines.push('deletion ' + older.names.get(deletion.node) + ' before ' + newer.names.get(deletion.at) + (wrap.length ? ' in ' + wrap.join(', ') : ''));
            }
        }
        lines.push('summary +' + result.added + ' −' + result.removed + ' in ' + result.changes.length);
        console.log(JSON.stringify(lines));
        """;

    private ServedLibrary library = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.library = new ServedLibrary("/js/markdown-diff.js", "/lib/codemirror/codemirror.min.js");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.library.Dispose();
    }

    [Test]
    public async Task AChangeOfFormatAloneIsNamedOverTheTextItCoversAndOnceAfterItsBlock()
    {
        var lines = await this.Diff("""
            [
              [["p", "Splatnost je 14 dní."], ["p", "Platba převodem.", { "marks": [[7, 15, { "emphasis": true }]] }],
               ["p", "Podle ceníku.", { "marks": [[6, 12, { "link": "https://a.example" }]] }], ["p", "Kód ABC a text."],
               ["p", "Záloha a doplatek."]],
              [["p", "Splatnost je 14 dní.", { "marks": [[13, 19, { "strong": true }]] }], ["p", "Platba převodem."],
               ["p", "Podle ceníku.", { "marks": [[6, 12, { "link": "https://b.example" }]] }],
               ["p", "Kód ABC a text.", { "marks": [[4, 7, { "inlineCode": true, "strike_through": true }]] }],
               ["p", "Záloha a doplatek.", { "marks": [[0, 6, { "strong": true }], [9, 17, { "strong": true }]] }]]
            ]
            """);

        string[] expected =
        [
            "newer format \"14 dní\": nově tučně",
            "newer named \"Splatnost je 14 dní.\": nově tučně",
            "newer format \"převodem\": dříve kurzívou",
            "newer named \"Platba převodem.\": dříve kurzívou",
            "newer format \"ceníku\": adresa odkazu https://a.example → https://b.example",
            "newer named \"Podle ceníku.\": adresa odkazu https://a.example → https://b.example",
            "newer format \"ABC\": nově přeškrtnuto, nově jako kód",
            "newer named \"Kód ABC a text.\": nově přeškrtnuto, nově jako kód",
            "newer format \"Záloha\": nově tučně",
            "newer format \"doplatek\": nově tučně",
            "newer named \"Záloha a doplatek.\": nově tučně",
            "summary +5 −5 in 1",
        ];

        Assert.That(lines, Is.EqualTo(expected), "a block names each change of format once on its label");
    }

    [Test]
    public async Task AChangeOfBlockTypeNamesBothTypesOnTheBlocksLabel()
    {
        var lines = await this.Diff("""
            [
              [["h2", "Poznámka"], ["p", "Bod"], ["p", "Úkol", { "in": ["task"] }], ["p", "Citát", { "in": ["quote"] }],
               ["code", "x = 1"], ["p", "Vnořený", { "in": ["bullet"] }], ["h3", "Termíny"]],
              [["p", "Poznámka"], ["p", "Bod", { "in": ["bullet"] }], ["p", "Úkol", { "in": ["done"] }], ["p", "Citát"],
               ["p", "x = 1"], ["p", "Vnořený", { "in": ["bullet", "bullet"] }],
               ["p", "Termíny", { "marks": [[0, 7, { "strong": true }]] }]]
            ]
            """);

        string[] expected =
        [
            "newer restyled \"Poznámka\"",
            "newer named \"Poznámka\": nadpis 2 → odstavec",
            "newer restyled \"Bod\"",
            "newer named \"Bod\": odstavec → odrážka",
            "newer restyled \"Úkol\"",
            "newer named \"Úkol\": úkol → splněný úkol",
            "newer restyled \"Citát\"",
            "newer named \"Citát\": citace → odstavec",
            "newer restyled \"x = 1\"",
            "newer named \"x = 1\": blok kódu → odstavec",
            "newer restyled \"Vnořený\"",
            "newer named \"Vnořený\": odrážka → odrážka 2. úrovně",
            "newer restyled \"Termíny\"",
            "newer format \"Termíny\": nově tučně",
            "newer named \"Termíny\": nadpis 3 → odstavec; nově tučně",
            "summary +7 −7 in 1",
        ];

        Assert.That(lines, Is.EqualTo(expected), "a block of another type and format names both changes on one label");
    }

    [Test]
    public async Task EqualBlocksFaceEachOtherAndTheRestIsChangedRemovedOrAdded()
    {
        var lines = await this.Diff("""
            [
              [["h1", "Smlouva"], ["p", "Cena díla je nízká."], ["p", "Beze změny."], ["p", "Odstavec, který zmizel."],
               ["p", "Závěr."]],
              [["h1", "Smlouva"], ["p", "Cena díla je vysoká."], ["p", "Beze změny."], ["p", "Závěr."],
               ["p", "Dodatek na konci."]]
            ]
            """);

        string[] expected =
        [
            "older deleted \"nízká\"",
            "older removed \"Odstavec, který zmizel.\"",
            "newer inserted \"vysoká\"",
            "newer added \"Dodatek na konci.\"",
            "deletion \"nízká\" before \"v\"",
            "deletion \"Odstavec, který zmizel.\" before \"Závěr.\"",
            "summary +2 −2 in 3",
        ];

        Assert.That(lines, Is.EqualTo(expected), "an equal block faces its equal even where a changed block would share more text with it");
    }

    [Test]
    public async Task TablesCompareRowsKnownByTheirFirstCellCellByCell()
    {
        var lines = await this.Diff("""
            [
              [["table", [["Termín", "Datum"], ["Zahájení", "2. 11."], ["Dokončení", "20. 11."], ["Převzetí", "30. 11."]]]],
              [["table", [["Termín", "Datum"], ["Zahájení", "2. 11."], ["Kontrola", "25. 11."], ["Dokončení", "27. 11."]]]]
            ]
            """);

        string[] expected =
        [
            "older deleted \"0\"",
            "older changed cell \"20. 11.\"",
            "older removed row \"Převzetí\"",
            "newer added row \"Kontrola\"",
            "newer inserted \"7\"",
            "newer changed cell \"27. 11.\"",
            "deletion \"0\" before \"7\"",
            "deletion row \"Převzetí\" before the end of the table",
            "summary +2 −2 in 1",
        ];

        Assert.That(lines, Is.EqualTo(expected), "a row is known by its first cell, and a changed row counts as one block on each side");
    }

    [Test]
    public async Task ARemovedListItemStandsWhereItWasInItsList()
    {
        var lines = await this.Diff("""
            [
              [["p", "první", { "in": ["bullet"] }], ["p", "druhý", { "in": ["bullet"] }], ["p", "třetí", { "in": ["bullet"] }],
               ["p", "poslední", { "in": ["bullet"] }], ["p", "Konec"]],
              [["p", "první", { "in": ["bullet"] }], ["p", "třetí", { "in": ["bullet"] }], ["p", "Konec"]]
            ]
            """);

        string[] expected =
        [
            "older removed \"druhý\"",
            "older removed \"poslední\"",
            "deletion \"druhý\" before list_item of \"třetí\" in list_item",
            "deletion \"poslední\" before \"Konec\" in bullet_list, list_item",
            "summary +0 −2 in 2",
        ];

        Assert.That(lines, Is.EqualTo(expected), "one column shows a removed item inside the list it stood in, as an item of its own");
    }

    private async Task<string[]> Diff(string specs)
    {
        return JsonSerializer.Deserialize<string[]>(await this.library.Run(Script, specs))!;
    }
}
