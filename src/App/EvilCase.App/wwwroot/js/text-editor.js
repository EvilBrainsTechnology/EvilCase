// The editor and the read-only view of a text file. CodeMirror is loaded from ../lib the first time either opens; the
// text leaves the browser only as what a save reads.
import { findMatches, foldText } from './text-search.js';

const editors = new Map();
const zoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4];
const typingDelay = 250;
let library = null;

export function loadLibrary() {
    library ??= import(new URL('../lib/codemirror/codemirror.min.js', import.meta.url).href).catch((error) => {
        library = null;
        throw error;
    });

    return library;
}

// Opens an editor of the text in host. Ctrl+S or Cmd+S anywhere in the dialog asks the owner to save. The owner hears
// NotifyChanged(dirty, canUndo, canRedo), NotifyMatches(current, total) of its search, NotifyFind on Ctrl+F and
// NotifySave.
export async function openEditor(host, text, language, indentUnit, label, owner) {
    const { createEditor } = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return;

    closeEditor(host);

    const editor = createEditor(host, text, language, indentUnit, label,
        (dirty, canUndo, canRedo) => owner.invokeMethodAsync('NotifyChanged', dirty, canUndo, canRedo),
        () => owner.invokeMethodAsync('NotifyFind'),
        (current, total) => owner.invokeMethodAsync('NotifyMatches', current, total));
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

    // Ctrl or Cmd with the wheel, and a pinch on a touchpad, zoom the text.
    (host.parentElement ?? host).addEventListener('wheel', (event) => {
        if (!event.ctrlKey && !event.metaKey) return;

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;

        setZoom(host, entry, Math.min(Math.max(entry.zoom * factor, zoomSteps[0]), zoomSteps.at(-1)));
    }, { passive: false, signal: stop.signal });

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

    const current = entry.zoom;
    const next = zoom === 'actual'
        ? 1
        : zoom === 'in'
            ? zoomSteps.find((step) => step > current + 0.005) ?? zoomSteps.at(-1)
            : zoom === 'out'
                ? zoomSteps.findLast((step) => step < current - 0.005) ?? zoomSteps[0]
                : current;

    setZoom(host, entry, next);
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

// An empty query clears the matches at once; a new query waits for the typing to pause. The search counts the matches
// in all of the text and wraps around.
export function findInViewer(host, query, again, previous) {
    const entry = editors.get(host);

    if (!entry?.viewer) return;

    clearTimeout(entry.typing);

    if (query.length === 0) {
        entry.query = '';
        entry.matches = [];
        entry.viewer.setMatches([]);
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
