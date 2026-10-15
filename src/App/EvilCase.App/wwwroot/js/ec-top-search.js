let field = null;
let fieldHandler = null;
let documentHandler = null;

// Ctrl K and Cmd K focus the field wherever the page is; the arrows and Enter never move its caret or submit.
export function attach(input) {
    detach();
    field = input;
    fieldHandler = (event) => {
        if (event.key === 'ArrowDown' || event.key === 'ArrowUp' || event.key === 'Enter') event.preventDefault();
    };
    documentHandler = (event) => {
        if (!(event.ctrlKey || event.metaKey) || event.altKey || event.key?.toLowerCase() !== 'k') return;
        event.preventDefault();
        field.focus();
        field.select();
    };
    field.addEventListener('keydown', fieldHandler);
    document.addEventListener('keydown', documentHandler);
}

export function detach() {
    if (field && fieldHandler) field.removeEventListener('keydown', fieldHandler);
    if (documentHandler) document.removeEventListener('keydown', documentHandler);
    field = null;
    fieldHandler = null;
    documentHandler = null;
}

export function scrollActiveIntoView(list) {
    list?.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: 'nearest' });
}
