// The native dialog traps focus and closes on Esc; its close event tells Blazor the state changed.
export function openModal(dialog, owner) {
    if (dialog.open) return;
    dialog.addEventListener('close', () => owner.invokeMethodAsync('NotifyClosed'), { once: true });
    dialog.showModal();
    dialog.querySelector('input, select, textarea, button')?.focus();
}

export function closeModal(dialog) {
    if (dialog.open) dialog.close();
}
