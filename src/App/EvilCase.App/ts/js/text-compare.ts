// The compare of two versions of a text file, read-only: text and code with the text editor's library, a Markdown text
// formatted with the Markdown library besides. The texts never leave the browser.
import { diffMarkdown } from './markdown-diff.js';
import { loadLibrary as loadMarkdownLibrary } from './markdown-editor.js';
import { debouncedFind, findMatches, foldText } from './text-search.js';
import { formatInWorker, loadLibrary } from './text-editor.js';
import type { FindEntry, FoldedText, Match } from './text-search.js';
import type { Language, Summary } from '../lib-src/codemirror.js';

type CodeCompare = ReturnType<typeof import('../lib-src/codemirror.js')['createCompare']>;

// A compare of either library.
interface Compare {
    matches(query: string): Match[][];
    top(index: number, pos: number): number;
    setMatches(ranges: Match[][]): void;
    showMatch(index: number, range: Match): void;
    changes(): number;
    showChange(number: number): void;
    destroy(): void;
    summary?: Summary;
}

interface Layout {
    parent: HTMLDivElement;
    unified: boolean;
    compare: Compare | null;
    building: Promise<Compare> | null;
}

interface CompareEntry extends FindEntry {
    host: HTMLElement;
    owner: DotNetObject;
    create: (layout: Layout, onFind: () => void) => Compare | Promise<Compare>;
    stop: AbortController;
    layouts: Layout[];
    active: Layout | null;
    matches: { index: number; range: Match; top: number }[];
    current: number;
    change: number;
    show?: (shown: boolean) => Summary;
}

const compares = new Map<HTMLElement, CompareEntry>();
const none = { formatted: false, formattable: false, added: 0, removed: 0, changes: 0 };

// A compare stands in two children of host, side by side and in one column; the stylesheet shows one of them, and each
// is built by create(layout, onFind) when it first shows. owner hears NotifyMatches(current, total) of the search and
// NotifyFind on Ctrl+F.
function start(host: HTMLElement, owner: DotNetObject, create: CompareEntry['create']) {
    closeCompare(host);

    const stop = new AbortController();
    const entry: CompareEntry = { host, owner, create, stop, layouts: [], active: null, query: '', matches: [], current: 0, change: -1, typing: null };

    compares.set(host, entry);

    return entry;
}

function layOut(entry: CompareEntry) {
    entry.layouts = [false, true].map((unified) => {
        const parent = entry.host.appendChild(document.createElement('div'));
        parent.className = unified ? 'ec-text-compare-unified' : 'ec-text-compare-split';
        return { parent, unified, compare: null, building: null };
    });

    const observer = new ResizeObserver(() => build(entry).catch(() => {}));
    for (const layout of entry.layouts) observer.observe(layout.parent);
    entry.stop.signal.addEventListener('abort', () => observer.disconnect());
}

// Opens the compare of older and newer in host, both formatted in language by the worker first with format; answers
// { formatted, formattable, added, removed, changes }. labels names the older, the newer and the one-column text.
export async function openCompare(host: HTMLElement, older: string, newer: string, language: Language | null, format: boolean, labels: string[], owner: DotNetObject) {
    const library = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return none;

    const original: [string, string] = [older, newer];
    let texts = original;
    let formatted: [string, string] | null = null;
    const entry = start(host, owner, (layout, onFind) => {
        const compare = library.createCompare(layout.parent, scroller(host), ...texts, language, layout.unified, labels, onFind) as CodeCompare & Pick<Compare, 'matches'>;
        let folded: FoldedText[] | null = null;
        compare.matches = (query) => {
            if (!query) return compare.texts().map(() => []);
            folded ??= compare.texts().map(foldText);
            return folded.map((text) => findMatches(text, query));
        };
        return compare;
    });

    if (format) {
        const answers = await Promise.all(original.map((text) => formatInWorker(text, language, entry.stop.signal)));

        // Closed while the texts formatted.
        if (compares.get(host) !== entry) return none;

        // Both sides compare formatted or neither does.
        if (answers.every((answer) => typeof answer.formatted === 'string')) formatted = answers.map((answer) => answer.formatted) as [string, string];
    }

    // Versions equal formatted show their original difference.
    const formattedSummary = formatted && library.compareSummary(...formatted);
    const shownFormatted = formattedSummary !== null && formattedSummary.changes > 0;
    texts = shownFormatted ? formatted! : original;

    // Shows the formatted or the original texts; the layouts are built again.
    entry.show = (shown) => {
        texts = shown && formatted !== null ? formatted : original;
        return library.compareSummary(...texts);
    };

    layOut(entry);
    await build(entry);

    return { formatted: shownFormatted, formattable: formatted !== null, ...(shownFormatted ? formattedSummary : library.compareSummary(...texts)) };
}

// Opens the compare of the Markdown texts older and newer in host, formatted; answers { added, removed, changes } in
// blocks. labels names the older, the newer and the one-column text.
export async function openMarkdownCompare(host: HTMLElement, older: string, newer: string, labels: string[], owner: DotNetObject) {
    const [library, markdown] = await Promise.all([loadLibrary(), loadMarkdownLibrary()]);

    // Closed while the libraries loaded.
    if (!host.isConnected) return none;

    const entry = start(host, owner, (layout, onFind) => markdown.createMarkdownCompare(
        layout.parent, scroller(host), older, newer, layout.unified, labels, onFind,
        (text, query) => findMatches(foldText(text), query),
        (olderBlocks, newerBlocks) => diffMarkdown(olderBlocks, newerBlocks, library.diffTexts)));

    layOut(entry);

    return { ...none, ...(await build(entry))?.summary };
}

function scroller(host: HTMLElement) {
    return host.closest<HTMLElement>('.ec-preview-stage') ?? host;
}

// Builds the layout the stylesheet shows, unless it is built, and answers its compare; a search runs again in the layout
// that came into view.
async function build(entry: CompareEntry): Promise<Compare | null> {
    const layout = entry.layouts.find((each) => each.parent.getClientRects().length > 0);

    if (!layout) return null;
    if (layout === entry.active) return layout.building;

    entry.active = layout;
    entry.change = -1;

    const building = (layout.building ??= Promise.resolve(entry.create(layout, () => entry.owner.invokeMethodAsync('NotifyFind').catch(() => {}))));
    const compare = await building;

    // Closed, or shown in other texts, while it was built.
    if (compares.get(entry.host) !== entry || layout.building !== building) {
        compare.destroy();
        return null;
    }

    layout.compare = compare;

    if (entry.active === layout) {
        if (entry.query) search(entry);
        else compare.setMatches(compare.matches(''));
    }

    return compare;
}

// Shows the formatted or the original texts; the layout is built again and an open search runs over it again. Answers
// { added, removed, changes } of the texts shown.
export async function showCompareFormatted(host: HTMLElement, formatted: boolean) {
    const entry = compares.get(host);

    if (!entry?.show) return { added: 0, removed: 0, changes: 0 };

    const summary = entry.show(formatted);

    for (const layout of entry.layouts) {
        layout.compare?.destroy();
        layout.compare = null;
        layout.building = null;
    }

    entry.active = null;
    await build(entry);

    return summary;
}

// Goes to the next or, with previous, the previous change and wraps around.
export function stepCompareChange(host: HTMLElement, previous: boolean) {
    const entry = compares.get(host);
    const compare = entry?.active?.compare;
    const count = compare?.changes() ?? 0;

    if (count === 0) return;

    entry!.change = previous ? (entry!.change <= 0 ? count - 1 : entry!.change - 1) : (entry!.change + 1) % count;
    compare!.showChange(entry!.change);
}

// The search counts the matches in every text the layout shows, in their order down the compare, and wraps around. A
// layout still being built searches the last query once it is built.
export function findInCompare(host: HTMLElement, query: string, again: boolean, previous: boolean) {
    const entry = compares.get(host);

    if (!entry?.active) return;

    const { compare } = entry.active;

    debouncedFind(entry, query, again, {
        clear: () => {
            entry.matches = [];
            compare?.setMatches(compare.matches(''));
        },
        search: () => search(entry),
        step: () => {
            const count = entry.matches.length;

            if (!compare) return;
            if (count > 0) show(entry, (entry.current + (previous ? count - 1 : 1)) % count);
            else report(entry);
        },
    });
}

function search(entry: CompareEntry) {
    const compare = entry.active?.compare;

    if (!compare) return;

    const found = compare.matches(entry.query);

    entry.matches = found
        .flatMap((ranges, index) => ranges.map((range) => ({ index, range, top: compare.top(index, range[0]) })))
        .sort((first, second) => first.top - second.top || first.index - second.index);
    compare.setMatches(found);

    if (entry.matches.length > 0) show(entry, 0);
    else report(entry);
}

function show(entry: CompareEntry, number: number) {
    const { index, range } = entry.matches[number];

    entry.current = number;
    entry.active!.compare!.showMatch(index, range);
    report(entry);
}

function report(entry: CompareEntry) {
    const count = entry.matches.length;

    entry.owner.invokeMethodAsync('NotifyMatches', count > 0 ? entry.current + 1 : 0, count).catch(() => {});
}

// Closes a compare, and stops a formatting still under way.
export function closeCompare(host: HTMLElement) {
    const entry = compares.get(host);
    if (!entry) return;
    compares.delete(host);
    entry.stop.abort();
    clearTimeout(entry.typing ?? undefined);
    for (const layout of entry.layouts) layout.compare?.destroy();
    host.replaceChildren();
}
