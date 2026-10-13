let handler = null;

// L opens the label box of the page unless a field, a menu or a dialog already has the keys.
export function listen(owner) {
    stop();
    handler = (event) => {
        if (event.defaultPrevented || event.ctrlKey || event.metaKey || event.altKey || event.key?.toLowerCase() !== 'l') return;
        const target = event.target;
        if (target instanceof Element && target.closest('input, textarea, select, [contenteditable]')) return;
        if (document.querySelector('dialog[open]')) return;
        event.preventDefault();
        owner.invokeMethodAsync('OpenFromShortcut');
    };
    document.addEventListener('keydown', handler);
}

export function stop() {
    if (handler) document.removeEventListener('keydown', handler);
    handler = null;
}

export function scrollActiveIntoView(list) {
    list?.querySelector('.ec-label-option-active')?.scrollIntoView({ block: 'nearest' });
}
