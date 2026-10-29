// The native dialog traps focus and closes on Esc; its close event tells Blazor the state changed.
// An element marked data-ec-keeps-escape handles Escape itself; the dialog must not close on it.
function keepEscape(event) {
    if (event.key === 'Escape' && event.target.closest('[data-ec-keeps-escape]')) event.preventDefault();
}

export function openModal(dialog, owner) {
    if (dialog.open) return;
    dialog.addEventListener('keydown', keepEscape);
    dialog.addEventListener('close', () => owner.invokeMethodAsync('NotifyClosed'), { once: true });
    dialog.showModal();
    dialog.querySelector('input:not([type=file]), select, textarea, button')?.focus();
}

export function closeModal(dialog) {
    if (dialog.open) dialog.close();
}
