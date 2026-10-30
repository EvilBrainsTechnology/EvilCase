// The native dialog traps focus and closes on Esc; its close event tells Blazor the state changed.
// A dialog marked data-ec-guarded does not close on Esc: Blazor hears the request and its owner decides.
// An element marked data-ec-keeps-escape handles Escape itself; the dialog must not close on it.
function keepEscape(event) {
    if (event.key === 'Escape' && event.target.closest('[data-ec-keeps-escape]')) event.preventDefault();
}

const owners = new WeakMap();

function requestClose(event) {
    const dialog = event.currentTarget;
    if (!dialog.hasAttribute('data-ec-guarded')) return;
    event.preventDefault();
    owners.get(dialog)?.invokeMethodAsync('NotifyCloseRequested');
}

export function openModal(dialog, owner) {
    if (dialog.open) return;
    owners.set(dialog, owner);
    dialog.addEventListener('cancel', requestClose);
    dialog.addEventListener('keydown', keepEscape);
    dialog.addEventListener('close', () => owner.invokeMethodAsync('NotifyClosed'), { once: true });
    dialog.showModal();
    dialog.querySelector('input:not([type=file]), select, textarea, button')?.focus();
}

export function closeModal(dialog) {
    if (dialog.open) dialog.close();
}
