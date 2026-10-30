// The editor of a text file. CodeMirror is loaded from ../lib the first time an editor opens; the text leaves the
// browser only as what a save reads.
const editors = new Map();
let library = null;

function loadLibrary() {
    library ??= import(new URL('../lib/codemirror/codemirror.min.js', import.meta.url).href).catch((error) => {
        library = null;
        throw error;
    });

    return library;
}

// Opens an editor of the text in host. The search field drives its search; Ctrl+S or Cmd+S anywhere in the dialog asks
// the owner to save. The owner hears NotifyChanged(dirty, canUndo, canRedo) and NotifySave.
export async function openEditor(host, searchInput, text, language, label, owner) {
    const { createEditor } = await loadLibrary();

    // Closed while the library loaded.
    if (!host.isConnected) return;

    closeEditor(host);

    const editor = createEditor(host, text, language, label,
        (dirty, canUndo, canRedo) => owner.invokeMethodAsync('NotifyChanged', dirty, canUndo, canRedo),
        () => { searchInput.focus(); searchInput.select(); });
    const stop = new AbortController();
    const { signal } = stop;

    searchInput.addEventListener('input', () => editor.search(searchInput.value), { signal });
    searchInput.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            if (event.shiftKey) editor.findPrevious(); else editor.findNext();
        } else if (event.key === 'Escape') {
            searchInput.value = '';
            editor.search('');
            editor.focus();
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

export function readEditorText(host) {
    return editors.get(host).editor.text();
}

export function undoEdit(host) {
    editors.get(host)?.editor.undo();
}

export function redoEdit(host) {
    editors.get(host)?.editor.redo();
}

export function closeEditor(host) {
    const entry = editors.get(host);
    if (!entry) return;
    editors.delete(host);
    entry.stop.abort();
    entry.editor.destroy();
}
