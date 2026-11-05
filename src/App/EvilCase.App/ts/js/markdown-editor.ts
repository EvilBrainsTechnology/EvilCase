// The Markdown view and editor of a file preview. Milkdown is loaded from ../lib the first time one opens; the text leaves
// the browser only as what a save reads.
import { loadStylesheet } from './preview-shared.js';
import { findMatches, foldText } from './text-search.js';

type MarkdownLibrary = typeof import('../lib-src/milkdown.js');
type Markdown = Awaited<ReturnType<MarkdownLibrary['createMarkdown']>>;

interface MarkdownEntry {
    markdown: Markdown | null;
    query: string;
    stop: AbortController;
}

const views = new Map<HTMLElement, MarkdownEntry>();
let library: Promise<MarkdownLibrary> | null = null;

export function loadLibrary() {
    library ??= Promise.all([
        import(new URL('../lib/milkdown/milkdown.min.js', import.meta.url).href),
        loadStylesheet(new URL('../lib/milkdown/prosemirror.css', import.meta.url).href),
    ]).then(([module]) => module as MarkdownLibrary).catch((error: unknown) => {
        library = null;
        throw error;
    });

    return library;
}

function markdownOf(host: HTMLElement) {
    const markdown = views.get(host)?.markdown;
    if (!markdown) throw new Error('The Markdown view is not open.');
    return markdown;
}

// Opens the text in host, read-only unless editable. While editable, Ctrl+S or Cmd+S anywhere in the dialog asks the owner
// to save. The owner hears NotifyChanged(dirty, active, disabled), NotifyMatches(current, total) of its search, NotifyFind
// on Ctrl+F and NotifySave.
export async function openMarkdown(host: HTMLElement, text: string, editable: boolean, label: string, owner: DotNetObject) {
    closeMarkdown(host);

    // Registered before the library loads, so a close or a query while it loads is heard.
    const entry: MarkdownEntry = { markdown: null, query: '', stop: new AbortController() };
    views.set(host, entry);

    let markdown: Markdown;
    try {
        const { createMarkdown } = await loadLibrary();

        // Closed while the library loaded.
        if (views.get(host) !== entry || !host.isConnected) {
            if (views.get(host) === entry) views.delete(host);
            return;
        }

        markdown = await createMarkdown(host, text, editable, label,
            (dirty, active, disabled) => owner.invokeMethodAsync('NotifyChanged', dirty, active, disabled),
            () => owner.invokeMethodAsync('NotifyFind'),
            (current, total) => owner.invokeMethodAsync('NotifyMatches', current, total),
            (blockText, query) => findMatches(foldText(blockText), query));
    } catch (error) {
        if (views.get(host) === entry) views.delete(host);
        throw error;
    }

    // Closed while it opened.
    if (views.get(host) !== entry || !host.isConnected) {
        if (views.get(host) === entry) views.delete(host);
        markdown.destroy();
        return;
    }

    entry.markdown = markdown;
    host.closest('dialog')?.addEventListener('keydown', (event) => {
        if (markdown.editable() && (event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === 's') {
            event.preventDefault();
            owner.invokeMethodAsync('NotifySave');
        }
    }, { signal: entry.stop.signal });

    if (editable) markdown.focus();
    if (entry.query) markdown.find(entry.query, false, false);
}

export function readMarkdown(host: HTMLElement) {
    return markdownOf(host).text();
}

export function editMarkdown(host: HTMLElement) {
    markdownOf(host).edit();
}

export function showMarkdown(host: HTMLElement, text: string) {
    markdownOf(host).show(text);
}

export function runMarkdownCommand(host: HTMLElement, command: string) {
    markdownOf(host).run(command);
}

export function readMarkdownLink(host: HTMLElement) {
    return markdownOf(host).link();
}

// The focus returns to the text once the link window has closed.
export function setMarkdownLink(host: HTMLElement, href: string) {
    const markdown = markdownOf(host);
    markdown.setLink(href);
    requestAnimationFrame(() => markdown.focus());
}

// A new query selects its first match from the cursor on; again steps to the next or, with previous, the previous match.
// A query that comes while the view opens is searched once it is open.
export function findInMarkdown(host: HTMLElement, query: string, again: boolean, previous: boolean) {
    const entry = views.get(host);
    if (!entry) return;
    if (entry.markdown) entry.markdown.find(query, again, previous);
    else entry.query = query;
}

export function focusMarkdown(host: HTMLElement) {
    views.get(host)?.markdown?.focus();
}

export function closeMarkdown(host: HTMLElement) {
    const entry = views.get(host);
    if (!entry) return;
    views.delete(host);
    entry.stop.abort();
    entry.markdown?.destroy();
}
