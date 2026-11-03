// The editor and the read-only view of a text file. CodeMirror is loaded from ../lib the first time either opens; the
// text leaves the browser only as what a save reads.
import { stepZoom, touchZoom, wheelZoom } from './preview-shared.js';
import { debouncedFind, findMatches, foldText } from './text-search.js';

const editors = new Map();
let library = null;

export function loadLibrary() {
    library ??= import(new URL('../lib/codemirror/codemirror.min.js', import.meta.url).href).catch((error) => {
        library = null;
        throw error;
    });

    return library;
}

// Opens an editor of the text in host. Ctrl+S or Cmd+S anywhere in the dialog asks the owner to save. The owner hears
// NotifyChanged(dirty, canUndo, canRedo), NotifyMatches(current, total) of its search, NotifyFind on Ctrl+F, NotifySave
// and NotifyCompared(added, removed, changes) of the changes it shows.
export async function openEditor(host, text, language, indentUnit, label, owner) {
    const { createEditor } = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return;

    closeEditor(host);

    const editor = createEditor(host, text, language, indentUnit, label,
        (dirty, canUndo, canRedo) => owner.invokeMethodAsync('NotifyChanged', dirty, canUndo, canRedo),
        () => owner.invokeMethodAsync('NotifyFind'),
        (current, total) => owner.invokeMethodAsync('NotifyMatches', current, total),
        (added, removed, changes) => owner.invokeMethodAsync('NotifyCompared', added, removed, changes));
    const stop = new AbortController();
    const { signal } = stop;

    // The first Escape in the text keeps the dialog open and lets Tab leave the editor until another key; a second
    // Escape reaches the dialog.
    let escaped = false;
    const release = () => {
        if (!escaped) return;
        escaped = false;
        editor.tabFocus(false);
    };
    host.addEventListener('focusin', release, { signal });
    host.addEventListener('keydown', (event) => {
        if (event.isComposing) return;
        if (event.key !== 'Escape') {
            if (!['Shift', 'Control', 'Alt', 'Meta'].includes(event.key)) release();
        } else if (!escaped || event.repeat) {
            escaped = true;
            editor.tabFocus(true);
            event.preventDefault();
        } else {
            release();
        }
    }, { signal });
    host.closest('dialog')?.addEventListener('keydown', (event) => {
        if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === 's') {
            event.preventDefault();
            owner.invokeMethodAsync('NotifySave');
        }
    }, { signal });

    editors.set(host, { editor, stop });
    editor.focus();
}

// Opens a read-only view of the text in host, formatted by the worker first with format; answers { formatted, invalid }.
// view, the preview toolbar's .NET reference, hears ZoomChanged, MatchesChanged and FocusSearch.
export async function openViewer(host, text, language, format, label, view) {
    const { createViewer } = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return { formatted: false, invalid: false };

    closeEditor(host);

    const stop = new AbortController();
    const entry = { viewer: null, stop, view, original: text, formatted: null, zoom: 1, folded: null, query: '', matches: [], current: 0, typing: null };

    editors.set(host, entry);

    const answer = format ? await formatInWorker(text, language, stop.signal) : {};

    // Closed while the text formatted.
    if (editors.get(host) !== entry) return { formatted: false, invalid: false };

    entry.formatted = answer.formatted ?? null;
    entry.viewer = createViewer(host, host.parentElement ?? host, entry.formatted ?? text, language, label,
        () => view.invokeMethodAsync('FocusSearch').catch(() => {}));

    wheelZoom(host.parentElement ?? host, stop.signal, () => entry.zoom, (zoom) => setZoom(host, entry, zoom));
    touchZoom(host.parentElement ?? host, stop.signal, () => entry.zoom, (zoom) => setZoom(host, entry, zoom));

    return { formatted: entry.formatted !== null, invalid: answer.invalid === true };
}

// Formats text in its language off the page's thread; answers { formatted }, { invalid: true } or { failed: true }. An
// abort of signal stops the worker.
export function formatInWorker(text, language, signal) {
    return new Promise((answer) => {
        let worker;

        try {
            worker = new Worker(new URL('./code-preview-worker.js', import.meta.url), { type: 'module' });
        } catch {
            answer({ failed: true });

            return;
        }

        const stop = () => finish({ failed: true });
        const finish = (data) => {
            worker.terminate();
            signal.removeEventListener('abort', stop);
            answer(data);
        };

        signal.addEventListener('abort', stop);
        worker.onmessage = ({ data }) => finish(data);
        worker.onerror = () => finish({ failed: true });
        worker.postMessage({ text, language });
    });
}

// Shows the formatted or the original text and runs an open search over it again; the zoom stays.
export function showFormatted(host, formatted) {
    const entry = editors.get(host);

    if (!entry?.viewer) return;

    entry.viewer.setText(formatted && entry.formatted !== null ? entry.formatted : entry.original);
    entry.folded = null;

    if (entry.query) search(entry);
}

// zoom is in, out or actual; the fits of a page do not apply to a text.
export function zoomViewer(host, zoom) {
    const entry = editors.get(host);

    if (!entry?.viewer) return;

    setZoom(host, entry, stepZoom(entry.zoom, zoom));
}

// The text size follows the zoom; the line at the top of the view stays there.
function setZoom(host, entry, zoom) {
    if (zoom === entry.zoom) return;

    const anchor = entry.viewer.firstVisible();

    entry.zoom = zoom;
    host.style.setProperty('--ec-preview-zoom', String(zoom));
    entry.viewer.remeasure(anchor);
    entry.view.invokeMethodAsync('ZoomChanged', Math.round(zoom * 100), '').catch(() => {});
}

// The search counts the matches in all of the text and wraps around.
export function findInViewer(host, query, again, previous) {
    const entry = editors.get(host);

    if (!entry?.viewer) return;

    debouncedFind(entry, query, again, {
        clear: () => {
            entry.matches = [];
            entry.viewer.setMatches([]);
        },
        search: () => search(entry),
        step: () => {
            const count = entry.matches.length;

            if (count > 0) show(entry, (entry.current + (previous ? count - 1 : 1)) % count);
            else report(entry);
        },
    });
}

// The first match of a new query is the first one not above the top of the view.
function search(entry) {
    entry.folded ??= foldText(entry.viewer.text());
    entry.matches = findMatches(entry.folded, entry.query);
    entry.viewer.setMatches(entry.matches);

    const top = entry.viewer.firstVisible();

    if (entry.matches.length > 0) show(entry, Math.max(entry.matches.findIndex(([from]) => from >= top), 0));
    else report(entry);
}

function show(entry, number) {
    entry.current = number;
    entry.viewer.showMatch(entry.matches[number]);
    report(entry);
}

function report(entry) {
    const count = entry.matches.length;

    entry.view.invokeMethodAsync('MatchesChanged', count > 0 ? entry.current + 1 : 0, count, false).catch(() => {});
}

// A new query selects its first match from the cursor on; again steps to the next or, with previous, the previous match.
export function findInEditor(host, query, again, previous) {
    const editor = editors.get(host)?.editor;

    if (!editor) return;

    if (!again) editor.search(query);
    else if (previous) editor.findPrevious();
    else editor.findNext();
}

// Shows the changes of the edited text against original, or with null hides them; answers { added, removed, changes }.
export function showEditorChanges(host, original) {
    return editors.get(host)?.editor?.showChanges(original) ?? { added: 0, removed: 0, changes: 0 };
}

// Moves the cursor to the next or, with previous, the previous change shown and wraps around.
export function stepEditorChange(host, previous) {
    editors.get(host)?.editor?.stepChange(previous);
}

export function focusEditor(host) {
    editors.get(host)?.editor?.focus();
}

export function readEditorText(host) {
    return editors.get(host).editor.text();
}

export function undoEdit(host) {
    editors.get(host)?.editor.undo();
}

export function redoEdit(host) {
    editors.get(host)?.editor.redo();
}

// Closes an editor or a view, and stops a formatting still under way.
export function closeEditor(host) {
    const entry = editors.get(host);
    if (!entry) return;
    editors.delete(host);
    entry.stop.abort();
    clearTimeout(entry.typing);
    (entry.editor ?? entry.viewer)?.destroy();
}
