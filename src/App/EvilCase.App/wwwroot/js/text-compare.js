// The compare of two versions of a text file, read-only: text and code with the text editor's library, a Markdown text
// formatted with the Markdown library besides. The texts never leave the browser.
import { diffMarkdown } from './markdown-diff.js';
import { loadLibrary as loadMarkdownLibrary } from './markdown-editor.js';
import { findMatches, foldText } from './text-search.js';
import { formatInWorker, loadLibrary } from './text-editor.js';

const compares = new Map();
const typingDelay = 250;
const none = { formatted: false, formattable: false, added: 0, removed: 0, changes: 0 };

// A compare stands in two children of host, side by side and in one column; the stylesheet shows one of them, and each
// is built by create(layout, onFind) when it first shows. owner hears NotifyMatches(current, total) of the search and
// NotifyFind on Ctrl+F.
function start(host, owner, create) {
    closeCompare(host);

    const stop = new AbortController();
    const entry = { host, owner, create, stop, layouts: [], active: null, query: '', matches: [], current: 0, change: -1, typing: null };

    compares.set(host, entry);

    return entry;
}

function layOut(entry) {
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
export async function openCompare(host, older, newer, language, format, labels, owner) {
    const library = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return none;

    const original = [older, newer];
    let texts = original;
    let formatted = null;
    const entry = start(host, owner, (layout, onFind) => {
        const compare = library.createCompare(layout.parent, scroller(host), ...texts, language, layout.unified, labels, onFind);
        let folded = null;
        compare.matches = (query) => (folded ??= compare.texts().map(foldText)).map((text) => findMatches(text, query));
        return compare;
    });

    if (format) {
        const answers = await Promise.all(original.map((text) => formatInWorker(text, language, entry.stop.signal)));

        // Closed while the texts formatted.
        if (compares.get(host) !== entry) return none;

        // Both sides compare formatted or neither does.
        if (answers.every((answer) => typeof answer.formatted === 'string')) formatted = answers.map((answer) => answer.formatted);
    }

    // Versions equal formatted show their original difference.
    const formattedSummary = formatted && library.compareSummary(...formatted);
    const shownFormatted = formattedSummary?.changes > 0;
    texts = shownFormatted ? formatted : original;

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
export async function openMarkdownCompare(host, older, newer, labels, owner) {
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

function scroller(host) {
    return host.closest('.ec-preview-stage') ?? host;
}

// Builds the layout the stylesheet shows, unless it is built, and answers its compare; a search runs again in the layout
// that came into view.
async function build(entry) {
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
export async function showCompareFormatted(host, formatted) {
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
export function stepCompareChange(host, previous) {
    const entry = compares.get(host);
    const compare = entry?.active?.compare;
    const count = compare?.changes() ?? 0;

    if (count === 0) return;

    entry.change = previous ? (entry.change <= 0 ? count - 1 : entry.change - 1) : (entry.change + 1) % count;
    compare.showChange(entry.change);
}

// An empty query clears the matches at once; a new query waits for the typing to pause. The search counts the matches
// in every text the layout shows, in their order down the compare, and wraps around.
export function findInCompare(host, query, again, previous) {
    const entry = compares.get(host);
    const compare = entry?.active?.compare;

    if (!compare) return;

    clearTimeout(entry.typing);

    if (query.length === 0) {
        entry.query = '';
        entry.matches = [];
        compare.setMatches(compare.matches(''));
    } else if (again && entry.query === query) {
        const count = entry.matches.length;

        if (count > 0) show(entry, (entry.current + (previous ? count - 1 : 1)) % count);
        else report(entry);
    } else if (again) {
        entry.query = query;
        search(entry);
    } else {
        entry.typing = setTimeout(() => {
            entry.query = query;
            search(entry);
        }, typingDelay);
    }
}

function search(entry) {
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

function show(entry, number) {
    const { index, range } = entry.matches[number];

    entry.current = number;
    entry.active.compare.showMatch(index, range);
    report(entry);
}

function report(entry) {
    const count = entry.matches.length;

    entry.owner.invokeMethodAsync('NotifyMatches', count > 0 ? entry.current + 1 : 0, count).catch(() => {});
}

// Closes a compare, and stops a formatting still under way.
export function closeCompare(host) {
    const entry = compares.get(host);
    if (!entry) return;
    compares.delete(host);
    entry.stop.abort();
    clearTimeout(entry.typing);
    for (const layout of entry.layouts) layout.compare?.destroy();
    host.replaceChildren();
}
