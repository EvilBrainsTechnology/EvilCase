// The compare of two versions of a text file, read-only. It loads with the text editor's library; the texts never leave
// the browser.
import { findMatches, foldText } from './text-search.js';
import { formatInWorker, loadLibrary } from './text-editor.js';

const compares = new Map();
const typingDelay = 250;

// Opens the compare of older and newer in host, both formatted in language by the worker first with format; answers
// { formatted, formattable, added, removed, changes }. The compare stands in two children of host, side by side and in
// one column; the stylesheet shows one of them, and each is built when it first shows. labels names the older, the newer
// and the one-column text. owner hears NotifyMatches(current, total) of the search and NotifyFind on Ctrl+F.
export async function openCompare(host, older, newer, language, format, labels, owner) {
    const library = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return { formatted: false, formattable: false, added: 0, removed: 0, changes: 0 };

    closeCompare(host);

    const stop = new AbortController();
    const entry = {
        library, host, language, labels, owner, stop, original: [older, newer], formatted: null, texts: [older, newer], layouts: [], active: null,
        folded: null, query: '', matches: [], current: 0, change: -1, typing: null,
    };

    compares.set(host, entry);

    if (format) {
        const answers = await Promise.all([older, newer].map((text) => formatInWorker(text, language, stop.signal)));

        // Closed while the texts formatted.
        if (compares.get(host) !== entry) return { formatted: false, formattable: false, added: 0, removed: 0, changes: 0 };

        // Both sides compare formatted or neither does.
        if (answers.every((answer) => typeof answer.formatted === 'string')) entry.formatted = answers.map((answer) => answer.formatted);
    }

    // Versions equal formatted show their original difference.
    const formattedSummary = entry.formatted && library.compareSummary(...entry.formatted);
    const shownFormatted = formattedSummary?.changes > 0;
    entry.texts = shownFormatted ? entry.formatted : entry.original;

    entry.layouts = [false, true].map((unified) => {
        const parent = host.appendChild(document.createElement('div'));
        parent.className = unified ? 'ec-text-compare-unified' : 'ec-text-compare-split';
        return { parent, unified, compare: null };
    });

    const observer = new ResizeObserver(() => build(entry));
    for (const layout of entry.layouts) observer.observe(layout.parent);
    stop.signal.addEventListener('abort', () => observer.disconnect());

    build(entry);

    return { formatted: shownFormatted, formattable: entry.formatted !== null, ...(shownFormatted ? formattedSummary : library.compareSummary(...entry.texts)) };
}

// Builds the layout the stylesheet shows, unless it is built; a search runs again in the layout that came into view.
function build(entry) {
    const layout = entry.layouts.find((each) => each.parent.getClientRects().length > 0);

    if (!layout || layout === entry.active) return;

    layout.compare ??= entry.library.createCompare(
        layout.parent, entry.host.closest('.ec-preview-stage') ?? entry.host, ...entry.texts, entry.language, layout.unified,
        entry.labels, () => entry.owner.invokeMethodAsync('NotifyFind').catch(() => {}));
    entry.active = layout;
    entry.change = -1;
    entry.folded = null;

    if (entry.query) search(entry);
    else layout.compare.setMatches(layout.compare.texts().map(() => []));
}

// Shows the formatted or the original texts; the layout is built again and an open search runs over it again. Answers
// { added, removed, changes } of the texts shown.
export function showCompareFormatted(host, formatted) {
    const entry = compares.get(host);

    if (!entry?.layouts.length) return { added: 0, removed: 0, changes: 0 };

    entry.texts = formatted && entry.formatted !== null ? entry.formatted : entry.original;

    for (const layout of entry.layouts) {
        layout.compare?.destroy();
        layout.compare = null;
    }

    entry.active = null;
    build(entry);

    return entry.library.compareSummary(...entry.texts);
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

    if (!entry?.active) return;

    clearTimeout(entry.typing);

    if (query.length === 0) {
        entry.query = '';
        entry.matches = [];
        entry.active.compare.setMatches(entry.active.compare.texts().map(() => []));
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
    if (!entry.active) return;

    const { compare } = entry.active;

    entry.folded ??= compare.texts().map(foldText);
    entry.matches = entry.folded
        .flatMap((folded, index) => findMatches(folded, entry.query).map((range) => ({ index, range, top: compare.top(index, range[0]) })))
        .sort((first, second) => first.top - second.top || first.index - second.index);
    compare.setMatches(entry.folded.map((_, index) => entry.matches.filter((match) => match.index === index).map((match) => match.range)));

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
