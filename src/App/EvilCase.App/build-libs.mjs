// Generates wwwroot/lib and wwwroot/fonts from node_modules: copies the browser builds and their
// license files, and bundles postal-mime, @kenjiuno/msgreader, file-type, the code formatting (Prettier
// with @prettier/plugin-xml), the text editor (CodeMirror) and the Markdown view and editor (Milkdown) with esbuild.
//
// Usage: node build-libs.mjs        (after `npm ci`; no parameters, any working directory)
// Replaces wwwroot/lib and wwwroot/fonts; both are gitignored.
// Exits 1 when node_modules is missing, a source file or license file is missing, or a package that
// ships in the output has a license outside the allow-list.

import { build, transform } from 'esbuild';
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { basename, dirname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const modules = join(root, 'node_modules');
const lib = join(root, 'wwwroot', 'lib');
const fonts = join(root, 'wwwroot', 'fonts');

const allowedLicenses = [/^MIT(-0)?$/, /^Apache-2\.0$/, /^BSD-/, /^ISC$/, /^OFL-1\.1$/, /^LGPL-3\.0(-only|-or-later)?$/, /^Zlib$/];
const licenseFiles = ['LICENSE', 'LICENSE.txt', 'LICENSE.md', 'LICENSE.markdown', 'LICENCE', 'license', 'license.md'];

function fail(message) {
    console.error(`build-libs: ${message}`);
    process.exit(1);
}

function packageRoot(name) {
    return join(modules, ...name.split('/'));
}

function readPackage(name) {
    const file = join(packageRoot(name), 'package.json');

    if (!existsSync(file)) {
        fail(`${name} is not installed; run npm ci first.`);
    }

    return JSON.parse(readFileSync(file, 'utf8'));
}

// An SPDX expression is allowed when every AND operand has an allowed OR alternative.
function isAllowed(expression) {
    return expression
        .replace(/[()]/g, ' ')
        .split(/\s+AND\s+/)
        .every((operand) => operand.split(/\s+OR\s+/).some((id) => allowedLicenses.some((pattern) => pattern.test(id.trim()))));
}

function checkLicense(name) {
    const { license, version } = readPackage(name);

    if (typeof license !== 'string' || !isAllowed(license)) {
        fail(`${name}@${version} has license "${license}", which is not in the allow-list.`);
    }
}

function licenseOf(dir) {
    const found = licenseFiles.map((file) => join(dir, file)).find((file) => existsSync(file));

    if (!found) {
        fail(`${dir} has no license file.`);
    }

    return found;
}

function source(name, ...path) {
    const file = join(packageRoot(name), ...path);

    if (!existsSync(file)) {
        fail(`${name} has no ${path.join('/')}.`);
    }

    return file;
}

function copy(name, from, to, filter = () => true) {
    mkdirSync(dirname(to), { recursive: true });
    cpSync(source(name, ...from.split('/')), to, { recursive: true, filter });
}

function copyLicense(name, to, ...path) {
    mkdirSync(dirname(to), { recursive: true });
    cpSync(path.length ? source(name, ...path) : licenseOf(packageRoot(name)), to);
}

// Bundles one entry and writes the licenses of every package that went into the bundle next to it.
async function bundle(entry, outDir, outFile, options) {
    const result = await build({
        stdin: { contents: entry, resolveDir: root },
        bundle: true,
        minify: true,
        platform: 'browser',
        target: 'es2022',
        legalComments: 'none',
        metafile: true,
        write: false,
        absWorkingDir: root,
        ...options,
    });

    mkdirSync(outDir, { recursive: true });
    writeFileSync(join(outDir, outFile), result.outputFiles[0].contents);

    const bundled = new Set();

    for (const input of Object.keys(result.metafile.inputs)) {
        const parts = resolve(root, input).split(sep);
        const index = parts.lastIndexOf('node_modules');

        if (index >= 0) {
            const scoped = parts[index + 1].startsWith('@');
            bundled.add(parts.slice(index + 1, index + (scoped ? 3 : 2)).join('/'));
        }
    }

    const licenses = [];

    for (const bundledName of [...bundled].sort()) {
        checkLicense(bundledName);
        licenses.push(`===== ${bundledName} ${readPackage(bundledName).version} =====\n${readFileSync(licenseOf(packageRoot(bundledName)), 'utf8').trim()}\n`);
    }

    writeFileSync(join(outDir, 'LICENSE.txt'), licenses.join('\n'));
}

if (!existsSync(modules)) {
    fail('node_modules is missing; run npm ci first.');
}

const manifest = JSON.parse(readFileSync(join(root, 'package.json'), 'utf8'));

for (const name of Object.keys(manifest.dependencies)) {
    checkLicense(name);
}

rmSync(lib, { recursive: true, force: true });
rmSync(fonts, { recursive: true, force: true });

copy('docx-preview', 'dist/docx-preview.min.js', join(lib, 'docx-preview', 'docx-preview.min.js'));
copyLicense('docx-preview', join(lib, 'docx-preview', 'LICENSE'));

copy('jszip', 'dist/jszip.min.js', join(lib, 'jszip', 'jszip.min.js'));
copyLicense('jszip', join(lib, 'jszip', 'LICENSE.txt'));

copy('xlsx', 'dist/xlsx.full.min.js', join(lib, 'sheetjs', 'xlsx.full.min.js'));
copyLicense('xlsx', join(lib, 'sheetjs', 'LICENSE.txt'));

copy('libheif-js', 'libheif-wasm/libheif-bundle.js', join(lib, 'libheif', 'libheif-bundle.js'));
copyLicense('libheif-js', join(lib, 'libheif', 'LICENSE-libheif-js.txt'));
copyLicense('libheif-js', join(lib, 'libheif', 'LICENSE-libheif.txt'), 'libheif-wasm', 'LICENSE');

// The legacy build runs in browsers without the newest JavaScript features; the PDF scripting engine is not used.
copy('pdfjs-dist', 'legacy/build/pdf.min.mjs', join(lib, 'pdfjs', 'pdf.min.mjs'));
copy('pdfjs-dist', 'legacy/build/pdf.worker.min.mjs', join(lib, 'pdfjs', 'pdf.worker.min.mjs'));
copy('pdfjs-dist', 'iccs', join(lib, 'pdfjs', 'iccs'));
copy('pdfjs-dist', 'standard_fonts', join(lib, 'pdfjs', 'standard_fonts'));
copy('pdfjs-dist', 'wasm', join(lib, 'pdfjs', 'wasm'), (file) => !basename(file).startsWith('quickjs-eval'));
// The viewer component of the same build, without the viewer application; its stylesheet reads the images beside it.
writeFileSync(join(lib, 'pdfjs', 'pdf_viewer.mjs'), (await transform(readFileSync(source('pdfjs-dist', 'legacy', 'web', 'pdf_viewer.mjs'), 'utf8'), { minify: true, format: 'esm', target: 'es2022' })).code);
writeFileSync(join(lib, 'pdfjs', 'pdf_viewer.css'), (await transform(readFileSync(source('pdfjs-dist', 'legacy', 'web', 'pdf_viewer.css'), 'utf8'), { loader: 'css', minify: true })).code);
copy('pdfjs-dist', 'legacy/web/images', join(lib, 'pdfjs', 'images'));
copyLicense('pdfjs-dist', join(lib, 'pdfjs', 'LICENSE'));

await bundle("export { default } from 'postal-mime';", join(lib, 'postal-mime'), 'postal-mime.min.js', { format: 'esm' });
await bundle("export { default as MsgReader } from '@kenjiuno/msgreader';", join(lib, 'msgreader'), 'msgreader.min.js', { format: 'iife', globalName: 'EcMsg' });
await bundle("export { fileTypeFromBuffer } from 'file-type';", join(lib, 'file-type'), 'file-type.min.js', { format: 'esm' });

// Formats JSON, XML, YAML and HTML. `json-stringify` keeps every JSON number literal and escape as
// written; XML keeps its whitespace; HTML leaves its embedded scripts and styles as they are.
const codePreview = `
import { format } from 'prettier/standalone';
import * as babel from 'prettier/plugins/babel';
import * as estree from 'prettier/plugins/estree';
import * as html from 'prettier/plugins/html';
import * as yaml from 'prettier/plugins/yaml';
import xml from '@prettier/plugin-xml';

const options = {
    json: { parser: 'json-stringify' },
    xml: { parser: 'xml', xmlWhitespaceSensitivity: 'preserve' },
    yaml: { parser: 'yaml' },
    html: { parser: 'html', embeddedLanguageFormatting: 'off' },
};

// Returns the formatted text, or null when the text is not valid in its language; any other failure throws.
// json-stringify also accepts JSON5, so JSON is first checked by JSON.parse.
export async function formatCode(text, language) {
    try {
        if (language === 'json') {
            JSON.parse(text);
        }

        return await format(text, { ...options[language], plugins: [babel, estree, html, yaml, xml] });
    } catch (error) {
        if (error instanceof SyntaxError) {
            return null;
        }

        throw error;
    }
}
`;

await bundle(codePreview, join(lib, 'code-preview'), 'code-preview.min.js', { format: 'esm' });

// The text editor and its read-only view: CodeMirror with line numbers, marks of the lines changed since the editor
// opened, history, a search driven from the app's own field, the marks of the app's search in the view, and the
// highlighting of JSON, XML, YAML, HTML and Markdown. It neither formats nor completes what is typed.
const textEditor = `
import { Compartment, EditorState, RangeSet, StateEffect, StateField } from '@codemirror/state';
import { Decoration, EditorView, GutterMarker, drawSelection, gutter, highlightActiveLine, highlightActiveLineGutter, highlightSpecialChars, keymap, lineNumbers } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap, indentLess, indentMore, insertNewlineKeepIndent, redo, redoDepth, undo, undoDepth } from '@codemirror/commands';
import { SearchQuery, findNext, findPrevious, openSearchPanel, search, setSearchQuery } from '@codemirror/search';
import { HighlightStyle, bracketMatching, indentUnit, syntaxHighlighting } from '@codemirror/language';
import { tags } from '@lezer/highlight';
import { json } from '@codemirror/lang-json';
import { xml } from '@codemirror/lang-xml';
import { yaml } from '@codemirror/lang-yaml';
import { html } from '@codemirror/lang-html';
import { markdown, markdownLanguage } from '@codemirror/lang-markdown';

// The classes are the app's; the app's stylesheet colours them.
const highlighting = HighlightStyle.define([
    { tag: [tags.propertyName, tags.tagName], class: 'ec-code-name' },
    { tag: [tags.attributeName, tags.typeName], class: 'ec-code-attribute' },
    { tag: [tags.string, tags.attributeValue], class: 'ec-code-string' },
    { tag: [tags.number, tags.bool, tags.null, tags.keyword, tags.atom], class: 'ec-code-literal' },
    { tag: [tags.punctuation, tags.meta], class: 'ec-code-punctuation' },
    { tag: tags.comment, class: 'ec-code-comment' },
]);

// Markdown's own marks: the HTML inside it keeps the classes above.
const markdownHighlighting = HighlightStyle.define([
    { tag: tags.heading, class: 'ec-code-heading' },
    { tag: tags.strong, class: 'ec-code-strong' },
    { tag: tags.emphasis, class: 'ec-code-emphasis' },
    { tag: tags.strikethrough, class: 'ec-code-strikethrough' },
    { tag: [tags.link, tags.url], class: 'ec-code-string' },
    { tag: tags.monospace, class: 'ec-code-literal' },
    { tag: [tags.processingInstruction, tags.contentSeparator, tags.labelName], class: 'ec-code-punctuation' },
    { tag: tags.quote, class: 'ec-code-comment' },
]);

const languages = {
    json: () => json(),
    xml: () => xml({ autoCloseTags: false }),
    yaml: () => yaml(),
    html: () => html({ autoCloseTags: false }),
    markdown: () => markdown({ base: markdownLanguage, addKeymap: false }),
};

// A line longer than this leaves the whole text without colours: its highlighted markup takes seconds to lay out.
const maxColouredLine = 10000;

function hasLongLine(text) {
    for (let start = 0; ;) {
        const end = text.indexOf('\\n', start);

        if ((end < 0 ? text.length : end) - start > maxColouredLine) return true;
        if (end < 0) return false;
        start = end + 1;
    }
}

// The highlighting of language; none for plain text or a text with a long line.
function colours(text, language) {
    if (!language || hasLongLine(text)) return [];

    return [languages[language](), syntaxHighlighting(highlighting), language === 'markdown' ? syntaxHighlighting(markdownHighlighting) : []];
}

// What the editor and the read-only view share; onFind runs on Ctrl+F or Cmd+F.
function shared(label, onFind) {
    return [
        lineNumbers(),
        highlightSpecialChars(),
        drawSelection(),
        EditorView.lineWrapping,
        EditorView.contentAttributes.of({ 'aria-label': label }),
        keymap.of([{ key: 'Mod-f', preventDefault: true, run: () => { onFind(); return true; } }]),
    ];
}

class ChangedLine extends GutterMarker {
    elementClass = 'ec-text-editor-changed';
}

const changedLine = new ChangedLine();

// Tab indents the lines of a selection over more than one line; otherwise it types the unit in place of the selection.
function indentOrInsert(view) {
    const { state } = view;
    if (state.selection.ranges.some((range) => state.doc.lineAt(range.from).number !== state.doc.lineAt(range.to).number)) {
        return indentMore(view);
    }

    view.dispatch(state.update(state.replaceSelection(state.facet(indentUnit)), { scrollIntoView: true, userEvent: 'input.type' }));
    return true;
}

// The starts of the lines changed since the editor opened; a line edited back to its original text stays marked.
const changedLines = StateField.define({
    create: () => RangeSet.empty,
    update(lines, transaction) {
        if (!transaction.docChanged) return lines;

        const { doc } = transaction.state;
        const added = [];
        let marked = lines.map(transaction.changes);

        transaction.changes.iterChangedRanges((fromA, toA, fromB, toB) => {
            const first = doc.lineAt(fromB);
            const last = doc.lineAt(toB);
            marked = marked.update({ filterFrom: first.from, filterTo: last.to, filter: () => false });
            for (let number = first.number; number <= last.number; number++) {
                added.push(changedLine.range(doc.line(number).from));
            }
        });

        return marked.update({ add: added, sort: true });
    },
});

// Opens an editor of the text in parent. language is json, xml, yaml, html, markdown or null for plain text; unit is
// what Tab types and indents by. onChange(dirty, canUndo, canRedo) hears when any of the three changes; onFind runs on
// Ctrl+F or Cmd+F.
export function createEditor(parent, text, language, unit, label, onChange, onFind) {
    const highlighted = colours(text, language);
    let initial = null;
    let reported = [false, false, false];

    const view = new EditorView({
        parent,
        state: EditorState.create({
            doc: text,
            extensions: [
                changedLines,
                gutter({ class: 'ec-text-editor-changes', markers: (editor) => editor.state.field(changedLines) }),
                shared(label, onFind),
                highlightActiveLineGutter(),
                history(),
                highlightActiveLine(),
                indentUnit.of(unit),
                // An empty panel: the search runs from the app's field; the open panel only turns the highlights on.
                search({ createPanel: () => ({ dom: document.createElement('div'), top: true }) }),
                // Enter keeps the indentation of the line as typed; it never indents by the syntax.
                keymap.of([
                    { key: 'Enter', run: insertNewlineKeepIndent, shift: insertNewlineKeepIndent },
                    { key: 'Tab', run: indentOrInsert, shift: indentLess },
                    ...defaultKeymap,
                    ...historyKeymap,
                ]),
                highlighted,
                highlighted.length > 0 ? bracketMatching() : [],
                EditorView.updateListener.of((update) => {
                    if (!update.docChanged) return;
                    const state = [!update.state.doc.eq(initial), undoDepth(update.state) > 0, redoDepth(update.state) > 0];
                    if (state.some((value, index) => value !== reported[index])) {
                        reported = state;
                        onChange(...state);
                    }
                }),
            ],
        }),
    });

    initial = view.state.doc;
    openSearchPanel(view);

    return {
        text: () => view.state.doc.toString(),
        undo: () => undo(view),
        redo: () => redo(view),
        focus: () => view.focus(),
        // While on, Tab moves the focus out of the editor.
        tabFocus: (on) => view.setTabFocusMode(on),
        search(query) {
            view.dispatch({ effects: setSearchQuery.of(new SearchQuery({ search: query, literal: true })) });
            if (query) {
                view.dispatch({ selection: { anchor: view.state.selection.main.from } });
                findNext(view);
            }
        },
        findNext: () => findNext(view),
        findPrevious: () => findPrevious(view),
        destroy: () => view.destroy(),
    };
}

// The marks of the app's search in a read-only view: every match, and the match shown.
const setMatches = StateEffect.define();
const setShownMatch = StateEffect.define();

function marks(effect, className) {
    const mark = Decoration.mark({ class: className });

    return StateField.define({
        create: () => Decoration.none,
        update(value, transaction) {
            for (const each of transaction.effects) {
                if (each.is(effect)) value = Decoration.set(each.value.map(([from, to]) => mark.range(from, to)));
            }

            return value;
        },
        provide: (field) => EditorView.decorations.from(field),
    });
}

const matchMarks = marks(setMatches, 'ec-text-editor-hit');
const shownMatchMarks = marks(setShownMatch, 'ec-text-editor-hit-current');
// A changed theme makes CodeMirror measure the size of its text again, as a zoom needs.
const measure = new Compartment();

// Opens a read-only view of the text in parent, highlighted as by createEditor: the text is selected and copied but
// never changed, and a touch opens no keyboard. onFind runs on Ctrl+F or Cmd+F.
export function createViewer(parent, text, language, label, onFind) {
    const state = (doc) => EditorState.create({
        doc,
        extensions: [
            gutter({ class: 'ec-text-editor-changes' }),
            shared(label, onFind),
            EditorState.readOnly.of(true),
            EditorState.transactionFilter.of((transaction) => (transaction.docChanged ? [] : transaction)),
            EditorView.contentAttributes.of({ 'aria-readonly': 'true', inputmode: 'none' }),
            keymap.of(defaultKeymap),
            colours(doc, language),
            matchMarks,
            shownMatchMarks,
            measure.of([]),
        ],
    });
    const view = new EditorView({ parent, state: state(text) });
    const inView = (from, to) => view.visibleRanges.some((range) => range.from <= from && to <= range.to);

    return {
        text: () => view.state.doc.toString(),
        setText: (doc) => view.setState(state(doc)),
        firstVisible: () => view.visibleRanges[0]?.from ?? 0,
        setMatches: (ranges) => view.dispatch({ effects: [setMatches.of(ranges), setShownMatch.of([])] }),
        // The match shown scrolls to the middle of the view when it is out of view.
        showMatch: ([from, to]) => view.dispatch({
            effects: [setShownMatch.of([[from, to]]), ...(inView(from, to) ? [] : [EditorView.scrollIntoView(from, { y: 'center' })])],
        }),
        remeasure: (anchor) => view.dispatch({ effects: [measure.reconfigure(EditorView.theme({})), EditorView.scrollIntoView(anchor, { y: 'start' })] }),
        destroy: () => view.destroy(),
    };
}
`;

await bundle(textEditor, join(lib, 'codemirror'), 'codemirror.min.js', { format: 'esm' });

// The Markdown view and editor: Milkdown with CommonMark, GFM, history and the clipboard, never Crepe. A link stays a link only
// to a web or e-mail address, images are never loaded, raw HTML never runs and a heading carries no id.
const markdownEditor = `
import { Editor, commandsCtx, defaultValueCtx, editorViewCtx, editorViewOptionsCtx, rootCtx, serializerCtx } from '@milkdown/kit/core';
import { commonmark, headingSchema, htmlSchema, imageSchema, linkSchema, remarkPreserveEmptyLinePlugin } from '@milkdown/kit/preset/commonmark';
import { gfm, insertTableCommand } from '@milkdown/kit/preset/gfm';
import { history } from '@milkdown/kit/plugin/history';
import { clipboard } from '@milkdown/kit/plugin/clipboard';
import { $prose, replaceAll } from '@milkdown/kit/utils';
import { Plugin } from '@milkdown/kit/prose/state';
import { redo, undo, undoDepth } from '@milkdown/kit/prose/history';
import { lift, setBlockType, toggleMark, wrapIn } from '@milkdown/kit/prose/commands';
import { liftListItem, wrapInList } from '@milkdown/kit/prose/schema-list';

// A link stays a link only to a web or e-mail address; any other shows as its text.
const safeLink = /^(https?:|mailto:)/i;

// Pasted HTML keeps only safe links; a Markdown link to anything else keeps its address in the file and shows as text.
const links = linkSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [{
        tag: 'a[href]',
        getAttrs: (dom) => (safeLink.test(dom.getAttribute('href') || '') ? { href: dom.getAttribute('href'), title: dom.getAttribute('title') } : false),
    }],
    toDOM: (mark) => (safeLink.test(mark.attrs.href)
        ? ['a', { href: mark.attrs.href, title: mark.attrs.title || null, target: '_blank', rel: 'noopener noreferrer' }, 0]
        : ['span', 0]),
}));

// An image is never loaded: it shows its description, or the word obrázek, and keeps its address in the file. A pasted
// img is dropped.
const images = imageSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [{
        tag: 'span[data-type="image"]',
        getAttrs: (dom) => ({ src: dom.getAttribute('data-src') || '', alt: dom.getAttribute('data-alt') || '', title: dom.getAttribute('data-title') || '' }),
    }],
    toDOM: (node) => ['span', {
        class: 'ec-markdown-image', 'data-type': 'image', 'data-src': node.attrs.src, 'data-alt': node.attrs.alt,
        'data-title': node.attrs.title, title: node.attrs.title || null,
    }, node.attrs.alt || 'obrázek'],
}));

// Raw HTML never runs: it shows as its text and stays in the file as written; pasted HTML never becomes raw HTML.
const html = htmlSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [],
    toDOM: (node) => ['span', { class: 'ec-markdown-html' }, node.attrs.value],
}));

// A heading carries no id, so the text of a file never names an element of the page.
const headings = headingSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    toDOM: (node) => ['h' + node.attrs.level, 0],
}));

function within(state, name) {
    const { $from } = state.selection;
    for (let depth = $from.depth; depth >= 0; depth--) {
        if ($from.node(depth).type.name === name) return true;
    }
    return false;
}

function headingLevel(state) {
    const { parent } = state.selection.$from;
    return parent.type.name === 'heading' ? parent.attrs.level : 0;
}

function markOn(name) {
    return (state) => {
        const type = state.schema.marks[name];
        const { from, to, empty, $from } = state.selection;
        return empty ? Boolean(type.isInSet(state.storedMarks || $from.marks())) : state.doc.rangeHasMark(from, to, type);
    };
}

// The nearest list around the selection and its kind: bullet, ordered or task.
function listAt(state) {
    const { $from } = state.selection;
    for (let depth = $from.depth; depth > 0; depth--) {
        const node = $from.node(depth);
        if (node.type.name === 'ordered_list') return { node, pos: $from.before(depth), kind: 'ordered' };
        if (node.type.name === 'bullet_list') {
            return { node, pos: $from.before(depth), kind: node.firstChild && node.firstChild.attrs.checked != null ? 'task' : 'bullet' };
        }
    }
    return null;
}

// Turns the nearest list around the selection of tr into a list of kind, its items with it.
function retype(tr, kind) {
    const found = listAt(tr);
    if (!found) return;
    const { nodes } = tr.doc.type.schema;
    tr.setNodeMarkup(found.pos, kind === 'ordered' ? nodes.ordered_list : nodes.bullet_list, { spread: found.node.attrs.spread });
    let pos = found.pos + 1;
    found.node.forEach((item) => {
        tr.setNodeMarkup(pos, undefined, {
            ...item.attrs,
            listType: kind === 'ordered' ? 'ordered' : 'bullet',
            checked: kind === 'task' ? item.attrs.checked === true : null,
        });
        pos += item.nodeSize;
    });
}

// The link around the cursor: its range and its address.
function linkAt(state) {
    const type = state.schema.marks.link;
    const { $from } = state.selection;
    const parent = $from.parent;
    let index = $from.index();
    let found = index < parent.childCount ? type.isInSet(parent.child(index).marks) : null;
    if (!found && $from.textOffset === 0 && index > 0) {
        index -= 1;
        found = type.isInSet(parent.child(index).marks);
    }
    if (!found) return null;
    let first = index;
    let last = index;
    while (first > 0 && found.isInSet(parent.child(first - 1).marks)) first -= 1;
    while (last + 1 < parent.childCount && found.isInSet(parent.child(last + 1).marks)) last += 1;
    let from = $from.start();
    for (let i = 0; i < first; i++) from += parent.child(i).nodeSize;
    let to = from;
    for (let i = first; i <= last; i++) to += parent.child(i).nodeSize;
    return { from, to, href: found.attrs.href };
}

function heading(level) {
    return (state, dispatch) => (headingLevel(state) === level
        ? setBlockType(state.schema.nodes.paragraph)(state, dispatch)
        : setBlockType(state.schema.nodes.heading, { level })(state, dispatch));
}

function mark(name) {
    return (state, dispatch) => toggleMark(state.schema.marks[name])(state, dispatch);
}

// A second press of the same list undoes it; another list turns into the pressed one.
function list(kind) {
    return (state, dispatch) => {
        const found = listAt(state);
        if (found && found.kind === kind) return liftListItem(state.schema.nodes.list_item)(state, dispatch);
        if (found) {
            if (dispatch) {
                const tr = state.tr;
                retype(tr, kind);
                dispatch(tr.scrollIntoView());
            }
            return true;
        }
        const type = kind === 'ordered' ? state.schema.nodes.ordered_list : state.schema.nodes.bullet_list;
        return wrapInList(type)(state, dispatch && ((tr) => {
            retype(tr, kind);
            dispatch(tr);
        }));
    };
}

function quote(state, dispatch) {
    return within(state, 'blockquote') ? lift(state, dispatch) : wrapIn(state.schema.nodes.blockquote)(state, dispatch);
}

// A selection within one paragraph becomes inline code, any other place a code block; a code block goes back to a paragraph.
function code(state, dispatch) {
    const { $from, $to, empty } = state.selection;
    if ($from.parent.type.name === 'code_block') return setBlockType(state.schema.nodes.paragraph)(state, dispatch);
    if (!empty && $from.sameParent($to)) return toggleMark(state.schema.marks.inlineCode)(state, dispatch);
    return setBlockType(state.schema.nodes.code_block)(state, dispatch);
}

// Which toolbar commands are on at the cursor.
const checks = {
    heading1: (state) => headingLevel(state) === 1,
    heading2: (state) => headingLevel(state) === 2,
    heading3: (state) => headingLevel(state) === 3,
    strong: markOn('strong'),
    emphasis: markOn('emphasis'),
    strike: markOn('strike_through'),
    bulletList: (state) => listAt(state)?.kind === 'bullet',
    orderedList: (state) => listAt(state)?.kind === 'ordered',
    taskList: (state) => listAt(state)?.kind === 'task',
    quote: (state) => within(state, 'blockquote'),
    code: (state) => within(state, 'code_block') || markOn('inlineCode')(state),
    link: (state) => linkAt(state) !== null,
};

// Opens the Markdown text in parent, read-only unless editable. While editable, onChange(dirty, active, disabled) hears
// whether the text changed since the edit began and which toolbar commands are on and unavailable at the cursor.
export async function createMarkdown(parent, text, editable, label, onChange) {
    const state = { editable, view: null, initial: null, reported: '', commands: null };

    const commands = {
        undo,
        redo,
        heading1: heading(1),
        heading2: heading(2),
        heading3: heading(3),
        strong: mark('strong'),
        emphasis: mark('emphasis'),
        strike: mark('strike_through'),
        bulletList: list('bullet'),
        orderedList: list('ordered'),
        taskList: list('task'),
        quote,
        code,
        // The address is asked by the app; here only whether a link can stand at the cursor.
        link: (s) => !within(s, 'code_block'),
        table: (s, dispatch) => {
            if (within(s, 'table') || within(s, 'code_block')) return false;
            if (!dispatch) return true;
            return state.commands.get(insertTableCommand.key)({ row: 3, col: 3 })(s, dispatch);
        },
    };

    const report = (force) => {
        const { view } = state;
        if (!view || !state.editable) return;
        const s = view.state;
        const dirty = undoDepth(s) > 0 && !s.doc.eq(state.initial);
        const active = Object.keys(checks).filter((name) => checks[name](s));
        const disabled = Object.keys(commands).filter((name) => !commands[name](s));
        const next = JSON.stringify([dirty, active, disabled]);
        if (!force && next === state.reported) return;
        state.reported = next;
        onChange(dirty, active, disabled);
    };

    const markBox = (box) => {
        box.tabIndex = state.editable ? 0 : -1;
        if (state.editable) box.removeAttribute('aria-readonly');
        else box.setAttribute('aria-readonly', 'true');
    };

    // A task item: a real checkbox, which only an edit can change.
    const taskItem = (node, view, getPos) => {
        if (node.attrs.checked == null) return null;
        let current = node;
        const dom = document.createElement('li');
        const holder = document.createElement('span');
        const box = document.createElement('input');
        const contentDOM = document.createElement('div');
        dom.className = 'ec-markdown-task';
        holder.className = 'ec-markdown-task-mark';
        holder.contentEditable = 'false';
        box.type = 'checkbox';
        box.className = 'ec-markdown-task-box';
        box.setAttribute('aria-label', 'Splněno');
        contentDOM.className = 'ec-markdown-task-text';
        holder.append(box);
        dom.append(holder, contentDOM);
        const paint = () => {
            box.checked = current.attrs.checked === true;
            dom.setAttribute('data-checked', String(current.attrs.checked === true));
            markBox(box);
        };
        box.addEventListener('mousedown', (event) => event.preventDefault());
        box.addEventListener('click', (event) => {
            if (!state.editable) event.preventDefault();
        });
        box.addEventListener('change', () => {
            const pos = getPos();
            if (!state.editable || typeof pos !== 'number') return;
            view.dispatch(view.state.tr.setNodeMarkup(pos, undefined, { ...current.attrs, checked: box.checked }));
        });
        paint();
        return {
            dom,
            contentDOM,
            update: (next) => {
                if (next.type !== current.type || next.attrs.checked == null) return false;
                current = next;
                paint();
                return true;
            },
            stopEvent: (event) => holder.contains(event.target),
            ignoreMutation: (mutation) => mutation.type !== 'selection'
                && (holder.contains(mutation.target) || (mutation.type === 'attributes' && mutation.target === dom)),
        };
    };

    const editor = await Editor.make()
        .config((ctx) => {
            ctx.set(rootCtx, parent);
            ctx.set(defaultValueCtx, text);
            ctx.update(editorViewOptionsCtx, (options) => ({
                ...options,
                editable: () => state.editable,
                attributes: () => (state.editable
                    ? { class: 'ec-markdown-content', role: 'textbox', 'aria-multiline': 'true', 'aria-label': label }
                    : { class: 'ec-markdown-content' }),
                nodeViews: { list_item: taskItem },
                // A dropped file is not inserted.
                handleDrop: (view, event) => Boolean(event.dataTransfer && event.dataTransfer.files.length > 0),
            }));
        })
        // An empty paragraph is not written as raw HTML.
        .use(commonmark.filter((plugin) => !remarkPreserveEmptyLinePlugin.includes(plugin)))
        .use(gfm)
        .use(history)
        .use(clipboard)
        .use(links)
        .use(images)
        .use(html)
        .use(headings)
        .use($prose(() => new Plugin({ view: () => ({ update: () => report(false) }) })))
        .create();

    const [view, serialize, manager] = editor.action((ctx) => [ctx.get(editorViewCtx), ctx.get(serializerCtx), ctx.get(commandsCtx)]);
    const markBoxes = () => {
        for (const box of view.dom.querySelectorAll('.ec-markdown-task-box')) markBox(box);
    };

    state.commands = manager;
    state.view = view;
    // Plugins have settled the document by now; a change is what comes after.
    state.initial = view.state.doc;
    // The read-only view is a document, not a text field.
    if (!state.editable) view.dom.removeAttribute('role');
    report(true);

    return {
        editable: () => state.editable,
        text: () => serialize(view.state.doc),
        focus: () => view.focus(),
        // Turns the shown text editable in place; a change is what comes after.
        edit() {
            state.editable = true;
            view.setProps({});
            markBoxes();
            state.initial = view.state.doc;
            report(true);
            view.focus();
        },
        // Shows the text read-only, without history.
        show(markdown) {
            state.editable = false;
            editor.action(replaceAll(markdown, true));
            view.setProps({});
            view.dom.removeAttribute('role');
            markBoxes();
            state.initial = view.state.doc;
        },
        run(name) {
            if (!Object.hasOwn(commands, name)) return;
            commands[name](view.state, view.dispatch);
            view.focus();
        },
        link() {
            const found = linkAt(view.state);
            return found ? found.href : '';
        },
        // Links the selection, the link at the cursor or, with nothing selected, the address itself; empty removes the link.
        setLink(href) {
            if (href && !safeLink.test(href)) return;
            const s = view.state;
            const type = s.schema.marks.link;
            const found = linkAt(s);
            const { from, to } = found && s.selection.empty ? found : s.selection;
            const tr = s.tr.removeMark(from, to, type);
            if (href && from === to) {
                tr.insertText(href, from);
                tr.addMark(from, from + href.length, type.create({ href }));
            } else if (href) {
                tr.addMark(from, to, type.create({ href }));
            }
            view.dispatch(tr.scrollIntoView());
        },
        destroy: () => editor.destroy(),
    };
}
`;

await bundle(markdownEditor, join(lib, 'milkdown'), 'milkdown.min.js', { format: 'esm' });
// The base rules of an editable ProseMirror text; the app's stylesheet loads after it and wins. tables.css is not used.
copy('@milkdown/prose', 'lib/style/prosemirror.css', join(lib, 'milkdown', 'prosemirror.css'));

for (const [family, name] of [['instrument-sans', '@fontsource-variable/instrument-sans'], ['jetbrains-mono', '@fontsource-variable/jetbrains-mono']]) {
    for (const subset of ['latin', 'latin-ext']) {
        copy(name, `files/${family}-${subset}-wght-normal.woff2`, join(fonts, `${family}-${subset}.woff2`));
    }

    copyLicense(name, join(fonts, `${family}-OFL.txt`));
}
