using System.Net;
using System.Text.Json;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MarkdownEditorLibraryTests
{
    // Opens each text read-only in the least DOM the Markdown library needs and prints the Markdown a save writes of it.
    private const string WriteScript = """
        class Node {
            nodeType = 1; childNodes = []; parentNode = null; attributes = new Map(); style = {};
            classList = { add: (name) => this.setAttribute('class', (this.className + ' ' + name).trim()), remove() {}, contains: () => false };
            constructor(name, value) { this.nodeName = name.toUpperCase(); if (value !== undefined) [this.nodeType, this.nodeValue] = [3, value]; }
            get ownerDocument() { return document; }
            get firstChild() { return this.childNodes[0] ?? null; }
            get lastChild() { return this.childNodes.at(-1) ?? null; }
            get parentElement() { return this.parentNode; }
            sibling(step) { const all = this.parentNode?.childNodes ?? []; return all[all.indexOf(this) + step] ?? null; }
            get nextSibling() { return this.sibling(1); }
            get previousSibling() { return this.sibling(-1); }
            get textContent() { return this.nodeValue ?? this.childNodes.map((child) => child.textContent).join(''); }
            set textContent(value) { this.childNodes = []; if (value) this.appendChild(new Node('#text', value)); }
            get className() { return this.getAttribute('class') ?? ''; }
            set className(value) { this.setAttribute('class', value); }
            insertBefore(node, before) { node.parentNode?.removeChild(node); this.childNodes.splice(before ? this.childNodes.indexOf(before) : this.childNodes.length, 0, node); node.parentNode = this; return node; }
            appendChild(node) { return this.insertBefore(node, null); }
            append(...nodes) { for (const node of nodes) this.appendChild(typeof node === 'string' ? new Node('#text', node) : node); }
            removeChild(node) { this.childNodes.splice(this.childNodes.indexOf(node), 1); node.parentNode = null; return node; }
            replaceChild(node, old) { this.insertBefore(node, old); return this.removeChild(old); }
            remove() { this.parentNode?.removeChild(this); }
            contains(node) { for (; node; node = node.parentNode) if (node === this) return true; return false; }
            getRootNode() { return this.parentNode?.getRootNode() ?? this; }
            setAttribute(name, value) { this.attributes.set(name, String(value)); }
            getAttribute(name) { return this.attributes.get(name) ?? null; }
            removeAttribute(name) { this.attributes.delete(name); }
            querySelectorAll() { return []; }
            getBoundingClientRect() { return { top: 0, left: 0, right: 0, bottom: 0, width: 0, height: 0 }; }
            addEventListener() {}
            removeEventListener() {}
        }
        const events = new EventTarget();
        globalThis.document = { body: new Node('body'), documentElement: new Node('html'), activeElement: null, getSelection: () => null, createElement: (name) => new Node(name), createTextNode: (value) => new Node('#text', value), addEventListener() {}, removeEventListener() {} };
        Object.assign(globalThis, { window: globalThis, innerWidth: 0, innerHeight: 0 });
        for (const name of ['addEventListener', 'removeEventListener', 'dispatchEvent']) globalThis[name] = events[name].bind(events);

        const { createMarkdown } = await import(process.argv[1]);
        const written = [];
        for (const text of JSON.parse(process.argv[2])) {
            const markdown = await createMarkdown(document.body.appendChild(new Node('div')), text, false, 'Text', () => {}, () => {}, () => {}, () => []);
            written.push(markdown.text());
        }
        console.log(JSON.stringify(written));
        """;

    [Test]
    public async Task AFormattedSaveWritesTheFrontMatterAsItStood()
    {
        const string valid = """
            ---
            # Údaje podání
            nazev:   Žádost o informace
            datum: 2026-01-05
            cislo: 0012
            stitky: [podání, lhůta]
            ucastnici:
              - jmeno: Petr Příklad
                role: 'podatel'
            poznamka: |
              první řádek
              druhý řádek
            ---

            """;
        const string invalid = """
            ---
            nazev: [Žádost
            ---

            """;
        using var library = new ServedLibrary("/lib/milkdown/milkdown.min.js");

        var written = JsonSerializer.Deserialize<string[]>(await library.Run(WriteScript, JsonSerializer.Serialize(new[] { valid + "# Žádost\n\nText.\n", invalid + "Text.\n" })))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written[0], Does.StartWith(valid), "the front matter is written back as it stood in the file");
            Assert.That(written[1], Does.StartWith(invalid), "a front matter that is not valid YAML is written back as it stood");
        }
    }

    [Test]
    public async Task TheMarkdownLibraryIsServedWithItsStylesheetAndTheLicensesOfWhatItBundles()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        using var library = await client.GetAsync(new Uri("/lib/milkdown/milkdown.min.js", UriKind.Relative));
        using var stylesheet = await client.GetAsync(new Uri("/lib/milkdown/prosemirror.css", UriKind.Relative));
        var licenses = await client.GetStringAsync(new Uri("/lib/milkdown/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the Markdown library is served");
            Assert.That(stylesheet.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the Markdown library's stylesheet is served");
            Assert.That(licenses, Does.Contain("===== @milkdown/core "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== @milkdown/prose "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== prosemirror-view "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== remark-gfm "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Not.Contain("===== vue "), "the bundle leaves out Milkdown's components");
            Assert.That(licenses, Does.Not.Contain("===== dompurify "), "the bundle leaves out Milkdown's components");
        }
    }

    [Test]
    public async Task TheMarkdownLibraryLinksOnlyToWebAndMailAddressesInANewTab()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var library = await client.GetStringAsync(new Uri("/lib/milkdown/milkdown.min.js", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library, Does.Contain("(https?:|mailto:)"), "a link stays a link only to a web or e-mail address");
            Assert.That(library, Does.Contain("noopener noreferrer"), "a link opens in a new tab without the opener");
        }
    }

    [Test]
    public async Task TheEditorLibraryHighlightsMarkdown()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var licenses = await client.GetStringAsync(new Uri("/lib/codemirror/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(licenses, Does.Contain("===== @codemirror/lang-markdown "), "the source editor highlights Markdown");
            Assert.That(licenses, Does.Contain("===== @lezer/markdown "), "the source editor highlights Markdown");
        }
    }
}
