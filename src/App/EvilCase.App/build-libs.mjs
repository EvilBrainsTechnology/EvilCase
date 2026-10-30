// Generates wwwroot/lib and wwwroot/fonts from node_modules: copies the browser builds and their
// license files, and bundles postal-mime, @kenjiuno/msgreader, file-type, the code preview (Prettier
// with @prettier/plugin-xml, and highlight.js) and the text editor (CodeMirror) with esbuild.
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

// Formats and highlights JSON, XML, YAML and HTML. `json-stringify` keeps every JSON number literal and escape as
// written; XML keeps its whitespace; HTML leaves its embedded scripts and styles as they are.
const codePreview = `
import { format } from 'prettier/standalone';
import * as babel from 'prettier/plugins/babel';
import * as estree from 'prettier/plugins/estree';
import * as html from 'prettier/plugins/html';
import * as yaml from 'prettier/plugins/yaml';
import xml from '@prettier/plugin-xml';
import hljs from 'highlight.js/lib/core';
import css from 'highlight.js/lib/languages/css';
import javascript from 'highlight.js/lib/languages/javascript';
import json from 'highlight.js/lib/languages/json';
import markup from 'highlight.js/lib/languages/xml';
import yamlLanguage from 'highlight.js/lib/languages/yaml';

hljs.registerLanguage('css', css);
hljs.registerLanguage('javascript', javascript);
hljs.registerLanguage('json', json);
hljs.registerLanguage('xml', markup);
hljs.registerLanguage('yaml', yamlLanguage);

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

export function highlightCode(text, language) {
    return hljs.highlight(text, { language: language === 'html' ? 'xml' : language, ignoreIllegals: true }).value;
}
`;

await bundle(codePreview, join(lib, 'code-preview'), 'code-preview.min.js', { format: 'esm' });

// The text editor: CodeMirror with line numbers, marks of the lines changed since it opened, history, a search driven
// from the app's own field and the highlighting of JSON, XML, YAML and HTML. It neither formats nor completes what is typed.
const textEditor = `
import { EditorState, RangeSet, StateField } from '@codemirror/state';
import { EditorView, GutterMarker, drawSelection, gutter, highlightActiveLine, highlightActiveLineGutter, highlightSpecialChars, keymap, lineNumbers } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap, indentLess, indentMore, insertNewlineKeepIndent, redo, redoDepth, undo, undoDepth } from '@codemirror/commands';
import { SearchQuery, findNext, findPrevious, openSearchPanel, search, setSearchQuery } from '@codemirror/search';
import { HighlightStyle, bracketMatching, indentUnit, syntaxHighlighting } from '@codemirror/language';
import { tags } from '@lezer/highlight';
import { json } from '@codemirror/lang-json';
import { xml } from '@codemirror/lang-xml';
import { yaml } from '@codemirror/lang-yaml';
import { html } from '@codemirror/lang-html';

// The classes are the app's; its stylesheet colours them like the code preview.
const highlighting = HighlightStyle.define([
    { tag: [tags.propertyName, tags.tagName], class: 'ec-code-name' },
    { tag: [tags.attributeName, tags.typeName], class: 'ec-code-attribute' },
    { tag: [tags.string, tags.attributeValue], class: 'ec-code-string' },
    { tag: [tags.number, tags.bool, tags.null, tags.keyword, tags.atom], class: 'ec-code-literal' },
    { tag: [tags.punctuation, tags.meta], class: 'ec-code-punctuation' },
    { tag: tags.comment, class: 'ec-code-comment' },
]);

const languages = {
    json: () => json(),
    xml: () => xml({ autoCloseTags: false }),
    yaml: () => yaml(),
    html: () => html({ autoCloseTags: false }),
};

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

// Opens an editor of the text in parent. language is json, xml, yaml, html or null for plain text; unit is what Tab
// types and indents by. onChange(dirty, canUndo, canRedo) hears when any of the three changes; onFind runs on Ctrl+F or
// Cmd+F.
export function createEditor(parent, text, language, unit, label, onChange, onFind) {
    let initial = null;
    let reported = [false, false, false];

    const view = new EditorView({
        parent,
        state: EditorState.create({
            doc: text,
            extensions: [
                changedLines,
                gutter({ class: 'ec-text-editor-changes', markers: (editor) => editor.state.field(changedLines) }),
                lineNumbers(),
                highlightActiveLineGutter(),
                highlightSpecialChars(),
                history(),
                drawSelection(),
                highlightActiveLine(),
                EditorView.lineWrapping,
                indentUnit.of(unit),
                EditorView.contentAttributes.of({ 'aria-label': label }),
                // An empty panel: the search runs from the app's field; the open panel only turns the highlights on.
                search({ createPanel: () => ({ dom: document.createElement('div'), top: true }) }),
                // Enter keeps the indentation of the line as typed; it never indents by the syntax.
                keymap.of([
                    { key: 'Mod-f', preventDefault: true, run: () => { onFind(); return true; } },
                    { key: 'Enter', run: insertNewlineKeepIndent, shift: insertNewlineKeepIndent },
                    { key: 'Tab', run: indentOrInsert, shift: indentLess },
                    ...defaultKeymap,
                    ...historyKeymap,
                ]),
                language ? [languages[language](), bracketMatching(), syntaxHighlighting(highlighting)] : [],
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
`;

await bundle(textEditor, join(lib, 'codemirror'), 'codemirror.min.js', { format: 'esm' });

for (const [family, name] of [['instrument-sans', '@fontsource-variable/instrument-sans'], ['jetbrains-mono', '@fontsource-variable/jetbrains-mono']]) {
    for (const subset of ['latin', 'latin-ext']) {
        copy(name, `files/${family}-${subset}-wght-normal.woff2`, join(fonts, `${family}-${subset}.woff2`));
    }

    copyLicense(name, join(fonts, `${family}-OFL.txt`));
}
