// The Markdown view and editor of a file preview. Milkdown is loaded from ../lib the first time one opens; the text leaves
// the browser only as what a save reads.
const views = new Map();
let library = null;

// The stylesheet goes before the app's own, so a rule of the app wins over one of the library.
function loadStylesheet(href) {
    return new Promise((resolve, reject) => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onload = () => resolve();
        link.onerror = () => {
            link.remove();
            reject(new Error(`The stylesheet ${href} did not load.`));
        };
        document.head.insertBefore(link, document.head.querySelector('link[rel="stylesheet"]'));
    });
}

function loadLibrary() {
    library ??= Promise.all([
        import(new URL('../lib/milkdown/milkdown.min.js', import.meta.url).href),
        loadStylesheet(new URL('../lib/milkdown/prosemirror.css', import.meta.url).href),
    ]).then(([module]) => module).catch((error) => {
        library = null;
        throw error;
    });

    return library;
}

function markdownOf(host) {
    const markdown = views.get(host)?.markdown;
    if (!markdown) throw new Error('The Markdown view is not open.');
    return markdown;
}

// Opens the text in host, read-only unless editable. While editable, Ctrl+S or Cmd+S anywhere in the dialog asks the owner
// to save. The owner hears NotifyChanged(dirty, active, disabled) and NotifySave.
export async function openMarkdown(host, text, editable, label, owner) {
    closeMarkdown(host);

    // Registered before the library loads, so a close while it loads is heard.
    const entry = { markdown: null, stop: new AbortController() };
    views.set(host, entry);

    let markdown;
    try {
        const { createMarkdown } = await loadLibrary();

        // Closed while the library loaded.
        if (views.get(host) !== entry || !host.isConnected) {
            if (views.get(host) === entry) views.delete(host);
            return;
        }

        markdown = await createMarkdown(host, text, editable, label,
            (dirty, active, disabled) => owner.invokeMethodAsync('NotifyChanged', dirty, active, disabled));
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
}

export function readMarkdown(host) {
    return markdownOf(host).text();
}

export function editMarkdown(host) {
    markdownOf(host).edit();
}

export function showMarkdown(host, text) {
    markdownOf(host).show(text);
}

export function runMarkdownCommand(host, command) {
    markdownOf(host).run(command);
}

export function readMarkdownLink(host) {
    return markdownOf(host).link();
}

// The focus returns to the text once the link window has closed.
export function setMarkdownLink(host, href) {
    const markdown = markdownOf(host);
    markdown.setLink(href);
    requestAnimationFrame(() => markdown.focus());
}

export function closeMarkdown(host) {
    const entry = views.get(host);
    if (!entry) return;
    views.delete(host);
    entry.stop.abort();
    entry.markdown?.destroy();
}
