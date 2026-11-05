// The text editor, its read-only view and the compare of two texts (CodeMirror): line numbers, changed-line marks,
// history, search marks and the highlighting of JSON, XML, YAML, HTML and Markdown. It neither formats nor completes what
// is typed. Its diff also compares the formatted Markdown texts.

import { Compartment, EditorState, RangeSet, RangeSetBuilder, StateEffect, StateField, Text } from '@codemirror/state';
import { Decoration, EditorView, GutterMarker, WidgetType, drawSelection, gutter, gutterLineClass, gutterWidgetClass, highlightActiveLine, highlightActiveLineGutter, highlightSpecialChars, keymap, lineNumbers } from '@codemirror/view';
import { Chunk, MergeView, diff, getChunks, getOriginalDoc, goToNextChunk, goToPreviousChunk, presentableDiff, unifiedMergeView } from '@codemirror/merge';
import { defaultKeymap, history, historyKeymap, indentLess, indentMore, insertNewlineKeepIndent, redo, redoDepth, selectAll, undo, undoDepth } from '@codemirror/commands';
import { SearchQuery, findNext, findPrevious, openSearchPanel, search, setSearchQuery } from '@codemirror/search';
import { HighlightStyle, bracketMatching, indentUnit, syntaxHighlighting } from '@codemirror/language';
import { tags } from '@lezer/highlight';
import { json } from '@codemirror/lang-json';
import { xml } from '@codemirror/lang-xml';
import { yaml } from '@codemirror/lang-yaml';
import { html } from '@codemirror/lang-html';
import { markdown, markdownLanguage } from '@codemirror/lang-markdown';
import type { Extension, Range, StateEffectType } from '@codemirror/state';
import type { DecorationSet } from '@codemirror/view';
import type { Change } from '@codemirror/merge';

// [from, to] positions of a text.
export type Match = [number, number];

// The lines a compare adds and removes, and its count of changes.
export interface Summary {
    added: number;
    removed: number;
    changes: number;
}

export type Language = keyof typeof languages;

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

function hasLongLine(text: string) {
    for (let start = 0; ;) {
        const end = text.indexOf('\n', start);

        if ((end < 0 ? text.length : end) - start > maxColouredLine) return true;
        if (end < 0) return false;
        start = end + 1;
    }
}

// The highlighting of language; none for plain text or a text with a long line.
function colours(text: string, language: Language | null): Extension[] {
    if (!language || hasLongLine(text)) return [];

    return [languages[language](), syntaxHighlighting(highlighting), language === 'markdown' ? syntaxHighlighting(markdownHighlighting) : []];
}

// What the editor and the read-only view share; onFind runs on Ctrl+F or Cmd+F.
function shared(label: string, onFind: () => void) {
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
function indentOrInsert(view: EditorView) {
    const { state } = view;
    if (state.selection.ranges.some((range) => state.doc.lineAt(range.from).number !== state.doc.lineAt(range.to).number)) {
        return indentMore(view);
    }

    view.dispatch(state.update(state.replaceSelection(state.facet(indentUnit)), { scrollIntoView: true, userEvent: 'input.type' }));
    return true;
}

// The starts of the lines changed since the editor opened; a line edited back to its original text stays marked.
const changedLines = StateField.define<RangeSet<GutterMarker>>({
    create: () => RangeSet.empty,
    update(lines, transaction) {
        if (!transaction.docChanged) return lines;

        const { doc } = transaction.state;
        const added: Range<GutterMarker>[] = [];
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
// Ctrl+F or Cmd+F; onMatches(current, total) hears the search's position and count while a query is set;
// onCompared(added, removed, changes) hears the summary of the changes shown against another text as it changes.
export function createEditor(parent: HTMLElement, text: string, language: Language | null, unit: string, label: string,
    onChange: (dirty: boolean, canUndo: boolean, canRedo: boolean) => void, onFind: () => void,
    onMatches: (current: number, total: number) => void, onCompared: (added: number, removed: number, changes: number) => void) {
    const highlighted = colours(text, language);
    let initial: Text | null = null;
    // The summary of the changes shown; null while none show.
    let compared: Summary | null = null;
    let reported = [false, false, false];
    let query: SearchQuery | null = null;
    // The starts of the query's matches in the text; null until counted.
    let starts: number[] | null = null;
    let counted = [0, 0];

    // The current match is the last one starting at or before the selection. A new query is reported even unchanged:
    // the field waits for its count.
    const report = (always?: boolean) => {
        let total = 0;
        let current = 0;

        if (query) {
            if (starts === null) {
                starts = [];
                for (const cursor = query.getCursor(view.state); ;) {
                    const next = cursor.next();
                    if (next.done) break;
                    starts.push(next.value.from);
                }
            }

            const at = view.state.selection.main.from;
            let low = 0;
            let high = starts.length;
            while (low < high) {
                const middle = (low + high) >> 1;
                if (starts[middle] <= at) low = middle + 1; else high = middle;
            }

            total = starts.length;
            current = total > 0 ? Math.max(low, 1) : 0;
        }

        if (always || current !== counted[0] || total !== counted[1]) {
            counted = [current, total];
            onMatches(current, total);
        }
    };

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
                changes.of([]),
                EditorView.updateListener.of((update) => {
                    if (compared !== null && getChunks(update.state)?.chunks !== getChunks(update.startState)?.chunks) {
                        const summary = editorSummary(update.state);
                        if (summary.added !== compared.added || summary.removed !== compared.removed || summary.changes !== compared.changes) {
                            compared = summary;
                            onCompared(summary.added, summary.removed, summary.changes);
                        }
                    }
                    if (update.docChanged) starts = null;
                    if (query && (update.docChanged || update.selectionSet)) report();
                    if (!update.docChanged) return;
                    const state: [boolean, boolean, boolean] = [!update.state.doc.eq(initial!), undoDepth(update.state) > 0, redoDepth(update.state) > 0];
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
        tabFocus: (on: boolean) => view.setTabFocusMode(on),
        search(text: string) {
            const next = new SearchQuery({ search: text, literal: true });
            query = next.valid ? next : null;
            starts = null;
            view.dispatch({ effects: setSearchQuery.of(next) });
            if (query) {
                view.dispatch({ selection: { anchor: view.state.selection.main.from } });
                findNext(view);
            }
            report(true);
        },
        findNext() {
            findNext(view);
            report();
        },
        findPrevious() {
            findPrevious(view);
            report();
        },
        // Shows the changes of the text against original in one column, the text staying editable, or with null hides
        // them; answers { added, removed, changes }. Long runs of unchanged lines collapse.
        showChanges(original: string | null): Summary {
            compared = null;
            view.dispatch({ effects: changes.reconfigure([]) });

            if (original === null) return { added: 0, removed: 0, changes: 0 };

            const expand = (run: number) => view.dispatch({ effects: expandRun.of(run) });
            view.dispatch({ effects: changes.reconfigure(editorChanges(original)) });
            view.dispatch({ effects: collapsedRuns(view.state.doc, getChunks(view.state)!.chunks, false, expand) });
            compared = editorSummary(view.state);
            return compared;
        },
        // Moves the cursor to the next or, with previous, the previous change and wraps around.
        stepChange(previous: boolean) {
            const chunks = getChunks(view.state)?.chunks ?? [];
            if (chunks.length === 0) return;
            // A cursor inside the only change stays there; the change comes into view.
            if (!(previous ? goToPreviousChunk : goToNextChunk)(view)) {
                view.dispatch({ selection: { anchor: chunks[0].fromB }, effects: EditorView.scrollIntoView(chunks[0].fromB, { y: 'center' }) });
            }
        },
        destroy: () => view.destroy(),
    };
}

// The changes the editor shows against another text; none until shown.
const changes = new Compartment();

// The editor's text against original in one column, marked as a one-column compare marks it.
function editorChanges(original: string) {
    return [
        unifiedMergeView({ original: Text.of(original.split(/\r\n?|\n/)), mergeControls: false, gutter: false, diffConfig }),
        runs,
        compareLines,
        gutterWidgetClass.of((view, widget) => (widget instanceof CollapsedRun ? null : removedLine)),
    ];
}

function editorSummary(state: EditorState) {
    return chunkSummary(getOriginalDoc(state), state.doc, getChunks(state)!.chunks);
}

// The marks of the app's search in a read-only view: every match, and the match shown.
const setMatches = StateEffect.define<Match[]>();
const setShownMatch = StateEffect.define<Match[]>();

function marks(effect: StateEffectType<Match[]>, className: string) {
    const mark = Decoration.mark({ class: className });

    return StateField.define<DecorationSet>({
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
// A changed theme makes CodeMirror measure the size of its text again, as a zoom needs; two themes take turns.
const measure = new Compartment();
const measureThemes = [EditorView.theme({}), EditorView.theme({})];

// A read-only text, in a view and in a compare: highlighted as by createEditor, selected and copied but never changed,
// with no cursor and no keyboard on a touch, and with the marks of the app's search.
function readOnly(doc: string, language: Language | null, label: string, onFind: () => void) {
    return [
        shared(label, onFind),
        EditorState.readOnly.of(true),
        EditorState.transactionFilter.of((transaction) => (transaction.docChanged ? [] : transaction)),
        EditorView.editable.of(false),
        // Focusable, so after a click the keys select all, copy, find and scroll the text.
        EditorView.contentAttributes.of({ tabindex: '0' }),
        EditorView.theme({ '.cm-cursorLayer': { display: 'none' } }),
        keymap.of([{ key: 'Mod-a', run: selectAll }]),
        colours(doc, language),
        matchMarks,
        shownMatchMarks,
    ];
}

// Whether the range of view stands whole inside the box of scroller.
function inView(view: EditorView, scroller: HTMLElement, from: number, to: number) {
    const box = scroller.getBoundingClientRect();
    const start = view.coordsAtPos(from, 1);
    const end = view.coordsAtPos(to, -1);

    return start !== null && end !== null && start.top >= box.top && end.bottom <= box.bottom;
}

// Opens a read-only view of the text in parent, highlighted as by createEditor. scroller is the element the view scrolls
// in; onFind runs on Ctrl+F or Cmd+F.
export function createViewer(parent: HTMLElement, scroller: HTMLElement, text: string, language: Language | null, label: string, onFind: () => void) {
    const state = (doc: string) => EditorState.create({
        doc,
        extensions: [gutter({ class: 'ec-text-editor-changes' }), readOnly(doc, language, label, onFind), measure.of([])],
    });
    const view = new EditorView({ parent, state: state(text) });
    let measured = 0;

    return {
        text: () => view.state.doc.toString(),
        setText: (doc: string) => view.setState(state(doc)),
        // The start of the line at the top of the scrolled view; the drawn ranges reach above it.
        firstVisible: () => view.lineBlockAtHeight(Math.max(scroller.getBoundingClientRect().top - view.documentTop, 0)).from,
        setMatches: (ranges: Match[]) => view.dispatch({ effects: [setMatches.of(ranges), setShownMatch.of([])] }),
        // The match shown scrolls to the middle of the view when it is out of view.
        showMatch: ([from, to]: Match) => view.dispatch({
            effects: [setShownMatch.of([[from, to]]), ...(inView(view, scroller, from, to) ? [] : [EditorView.scrollIntoView(from, { y: 'center' })])],
        }),
        remeasure: (anchor: number) => view.dispatch({ effects: [measure.reconfigure(measureThemes[measured++ % 2]), EditorView.scrollIntoView(anchor, { y: 'start', yMargin: 0 })] }),
        destroy: () => view.destroy(),
    };
}

// The diff of a compare and of its summary; the merge view's own default.
const diffConfig = { scanLimit: 500 };

// The changes of newer against older as [{ fromA, toA, fromB, toB }]: as a compare shows them, or with exact the fewest,
// whatever the characters mean. The formatted Markdown compare diffs its texts with it.
export function diffTexts(older: string, newer: string, exact?: boolean): readonly Change[] {
    return exact ? diff(older, newer, diffConfig) : presentableDiff(older, newer, diffConfig);
}

// The unchanged lines kept beside a change, and the fewest a collapsed run hides.
const keptLines = 3;
const collapsedLines = 5;

const setRuns = StateEffect.define<DecorationSet>();
const expandRun = StateEffect.define<number>();

// A collapsed run of unchanged lines; a click shows them in every view of the compare.
class CollapsedRun extends WidgetType {
    declare run: number;
    declare lines: number;
    declare expand: (run: number) => void;

    constructor(run: number, lines: number, expand: (run: number) => void) {
        super();
        this.run = run;
        this.lines = lines;
        this.expand = expand;
    }

    eq(other: CollapsedRun) {
        return other.run === this.run && other.lines === this.lines;
    }

    toDOM() {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'ec-text-compare-run';
        button.textContent = '… ' + this.lines + ' nezměněných řádků';
        button.setAttribute('aria-label', 'Zobrazit ' + this.lines + ' nezměněných řádků');
        button.addEventListener('click', () => this.expand(this.run));
        return button;
    }

    ignoreEvent() {
        return true;
    }

    get estimatedHeight() {
        return 32;
    }
}

// The collapsed runs of a text; an edit that touches a run and a match the editor's search selects inside one show it.
const runs = StateField.define<DecorationSet>({
    create: () => Decoration.none,
    update(value, transaction) {
        if (transaction.docChanged) value = value.update({ filter: (from, to) => !transaction.changes.touchesRange(from, to) }).map(transaction.changes);

        for (const effect of transaction.effects) {
            if (effect.is(setRuns)) value = effect.value;
            if (effect.is(expandRun)) value = value.update({ filter: (from, to, decoration) => decoration.spec.run !== effect.value });
        }

        if (transaction.isUserEvent('select.search')) {
            const { head } = transaction.state.selection.main;
            value = value.update({ filter: (from, to) => head < from || head > to });
        }

        return value;
    },
    provide: (field) => EditorView.decorations.from(field),
});

// The changed lines' marks for the gutters and the lines wholly added or removed, in step with the chunks of the compare.
const compareLines = StateField.define<{ gutters: RangeSet<GutterMarker>; lines: DecorationSet }>({
    create: changedLineMarks,
    update: (value, transaction) => (getChunks(transaction.state)?.chunks === getChunks(transaction.startState)?.chunks ? value : changedLineMarks(transaction.state)),
    provide: (field) => [gutterLineClass.from(field, (value) => value.gutters), EditorView.decorations.from(field, (value) => value.lines)],
});

class LineClass extends GutterMarker {
    constructor(elementClass: string) {
        super();
        this.elementClass = elementClass;
    }
}

const removedLine = new LineClass('ec-text-compare-removed');
const addedLine = new LineClass('ec-text-compare-added');
const gapLine = new LineClass('ec-text-compare-gap');
// A line of a chunk the other side has no text for: it is wholly added or removed, with no word marked.
const wholeLine = Decoration.line({ class: 'ec-text-compare-whole' });

// The positions of a chunk on one side of the compare.
function range(chunk: Chunk, older: boolean): [number, number, number] {
    return older ? [chunk.fromA, chunk.toA, chunk.endA] : [chunk.fromB, chunk.toB, chunk.endB];
}

// The runs of unchanged lines between the chunks that collapse, numbered in order on each side alike.
function collapsedRuns(doc: Text, chunks: readonly Chunk[], older: boolean, expand: (run: number) => void) {
    const collapsed = new RangeSetBuilder<Decoration>();
    let previous = 1;

    for (let index = 0; ; index++) {
        const chunk = index < chunks.length ? chunks[index] : null;
        const first = index > 0 ? previous + keptLines : 1;
        const last = chunk ? doc.lineAt(range(chunk, older)[0]).number - 1 - keptLines : doc.lines;

        if (last - first + 1 >= collapsedLines) {
            collapsed.add(doc.line(first).from, doc.line(last).to, Decoration.replace({
                widget: new CollapsedRun(index, last - first + 1, expand), block: true, run: index,
            }));
        }

        if (!chunk) break;

        previous = doc.lineAt(Math.min(doc.length, range(chunk, older)[1])).number;
    }

    return setRuns.of(collapsed.finish());
}

// The changed lines' marks for the gutters, and the lines of a chunk the other side has no text for.
function changedLineMarks(state: EditorState) {
    const { chunks, side } = getChunks(state) ?? { chunks: [], side: 'b' };
    const { doc } = state;
    const older = side === 'a';
    const gutters = new RangeSetBuilder<GutterMarker>();
    const lines = new RangeSetBuilder<Decoration>();

    for (const chunk of chunks) {
        const [from, to, end] = range(chunk, older);
        const [otherFrom, otherTo] = range(chunk, !older);

        if (to > from) {
            for (let line = doc.lineAt(from).number; line <= doc.lineAt(end).number; line++) {
                const start = doc.line(line).from;
                if (otherTo === otherFrom) lines.add(start, start, wholeLine);
                gutters.add(start, start, older ? removedLine : addedLine);
            }
        }
    }

    return { gutters: gutters.finish(), lines: lines.finish() };
}

// Counts the changed lines of newer against older as a compare shows them: { added, removed, changes }. A difference in
// line breaks alone is none.
export function compareSummary(older: string, newer: string) {
    const a = Text.of(older.split(/\r\n?|\n/));
    const b = Text.of(newer.split(/\r\n?|\n/));

    return chunkSummary(a, b, Chunk.build(a, b, diffConfig));
}

// The lines the chunks of b against a add and remove, and their count.
function chunkSummary(a: Text, b: Text, chunks: readonly Chunk[]): Summary {
    const lines = (doc: Text, from: number, to: number, end: number) => (to > from ? doc.lineAt(end).number - doc.lineAt(from).number + 1 : 0);

    return {
        added: chunks.reduce((sum, chunk) => sum + lines(b, chunk.fromB, chunk.toB, chunk.endB), 0),
        removed: chunks.reduce((sum, chunk) => sum + lines(a, chunk.fromA, chunk.toA, chunk.endA), 0),
        changes: chunks.length,
    };
}

// Opens a read-only compare of older and newer in parent: side by side, or with unified in one column with the removed
// lines above the added ones. Both texts are highlighted as by createViewer; changed words are marked and long runs of
// unchanged lines collapse. labels names the older, the newer and the one-column text; scroller is the element the
// compare scrolls in; onFind runs on Ctrl+F or Cmd+F.
export function createCompare(parent: HTMLElement, scroller: HTMLElement, older: string, newer: string, language: Language | null, unified: boolean, labels: string[],
    onFind: () => void) {
    const views: EditorView[] = [];
    const expand = (run: number) => {
        for (const view of views) view.dispatch({ effects: expandRun.of(run) });
    };
    // The block widgets beside the text are the removed lines in one column and the gaps facing them side by side.
    const widgetClass = (widgetLine: GutterMarker) => gutterWidgetClass.of((view, widget) => (widget instanceof CollapsedRun ? null : widgetLine));
    let merge: MergeView | null = null;

    if (unified) {
        views.push(new EditorView({
            parent,
            state: EditorState.create({
                doc: newer,
                extensions: [
                    readOnly(newer, language, labels[2], onFind),
                    unifiedMergeView({ original: older, mergeControls: false, gutter: false, diffConfig }),
                    runs,
                    compareLines,
                    widgetClass(removedLine),
                ],
            }),
        }));
    } else {
        const side = (doc: string, label: string) => ({ doc, extensions: [readOnly(doc, language, label, onFind), runs, compareLines, widgetClass(gapLine)] });
        merge = new MergeView({ parent, a: side(older, labels[0]), b: side(newer, labels[1]), gutter: false, diffConfig });
        views.push(merge.a, merge.b);
    }

    for (const view of views) {
        const { chunks, side } = getChunks(view.state)!;
        view.dispatch({ effects: collapsedRuns(view.state.doc, chunks, side === 'a', expand) });
    }

    // The view that shows the newer text, where a change is gone to.
    const newest = views.at(-1)!;

    return {
        // The texts the search goes through, one for each view: the older and the newer side by side, the newer alone in
        // one column.
        texts: () => views.map((view) => view.state.doc.toString()),
        // How far down the text a position of a view stands; the views of a compare stand level.
        top: (index: number, pos: number) => views[index].lineBlockAt(pos).top,
        setMatches: (ranges: Match[][]) => views.forEach((view, index) => view.dispatch({ effects: [setMatches.of(ranges[index]), setShownMatch.of([])] })),
        // A match inside a collapsed run shows the run first; the match comes to the middle of the view when it is out of view.
        showMatch(index: number, [from, to]: Match) {
            const view = views[index];
            view.state.field(runs).between(from, from, (start, end, decoration) => {
                if (decoration.spec.run !== undefined) expand(decoration.spec.run);
            });
            views.forEach((each, other) => each.dispatch({ effects: setShownMatch.of(other === index ? [[from, to]] : []) }));
            if (!inView(view, scroller, from, to)) view.dispatch({ effects: EditorView.scrollIntoView(from, { y: 'center' }) });
        },
        changes: () => getChunks(newest.state)!.chunks.length,
        showChange: (number: number) => newest.dispatch({ effects: EditorView.scrollIntoView(getChunks(newest.state)!.chunks[number].fromB, { y: 'center' }) }),
        destroy: () => (merge ?? views[0]).destroy(),
    };
}
