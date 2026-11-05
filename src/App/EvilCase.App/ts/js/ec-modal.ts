// The native dialog traps focus and closes on Esc; its close event tells Blazor the state changed.
// A dialog marked data-ec-guarded does not close on Esc: Blazor hears the request and its owner decides.
// An element marked data-ec-keeps-escape handles Escape itself; the dialog must not close on it.
function keepEscape(event: KeyboardEvent) {
    if (event.key === 'Escape' && (event.target as Element).closest('[data-ec-keeps-escape]')) event.preventDefault();
}

const owners = new WeakMap<HTMLDialogElement, DotNetObject>();

function requestClose(event: Event) {
    const dialog = event.currentTarget as HTMLDialogElement;
    if (!dialog.hasAttribute('data-ec-guarded')) return;
    event.preventDefault();
    owners.get(dialog)?.invokeMethodAsync('NotifyCloseRequested');
}

// On desktop a double-click on the header of a dialog marked data-ec-maximizable, outside its controls, toggles its size.
const desktop = matchMedia('(min-width: 992px)');

function toggleSize(event: MouseEvent) {
    const dialog = event.currentTarget as HTMLDialogElement;
    const target = event.target as Element;
    if (!dialog.hasAttribute('data-ec-maximizable') || !desktop.matches) return;
    if (!target.closest('.ec-modal-header')) return;
    if (target.closest('a, button, input, select, textarea, [contenteditable], .ec-file-rename, .ec-menu-anchor, .ec-popover-anchor')) return;
    // The double-click selected a word of the title.
    window.getSelection()?.removeAllRanges();
    owners.get(dialog)?.invokeMethodAsync('ToggleMaximized');
}

// The page does not scroll while any dialog is open; the last one to close returns it to where it stood.
const locks = new Set<HTMLDialogElement>();
let page: { href: string; x: number; y: number } | null = null;

function lockScroll(dialog: HTMLDialogElement) {
    if (locks.size === 0) {
        page = { href: location.href, x: window.scrollX, y: window.scrollY };
        document.documentElement.classList.add('ec-scroll-locked');
    }

    locks.add(dialog);
}

function unlockScroll(dialog: HTMLDialogElement) {
    if (!locks.delete(dialog) || locks.size > 0) return;

    document.documentElement.classList.remove('ec-scroll-locked');

    if (location.href === page!.href && (window.scrollX !== page!.x || window.scrollY !== page!.y)) window.scrollTo(page!.x, page!.y);
}

// Answers the dialog's lock of the page scroll; its release frees a dialog removed while open, which fires no close,
// and hands the focus back to the element that held it before the dialog opened, as a close does.
export function openModal(dialog: HTMLDialogElement, owner: DotNetObject) {
    let opener: HTMLElement | null = null;

    if (!dialog.open) {
        opener = document.activeElement as HTMLElement | null;
        owners.set(dialog, owner);
        dialog.addEventListener('cancel', requestClose);
        dialog.addEventListener('keydown', keepEscape);
        dialog.addEventListener('dblclick', toggleSize);
        dialog.addEventListener('close', () => {
            unlockScroll(dialog);
            owner.invokeMethodAsync('NotifyClosed');
        }, { once: true });
        dialog.showModal();
        dialog.querySelector<HTMLElement>('input:not([type=file]), select, textarea, button')?.focus();
    }

    lockScroll(dialog);

    return {
        release: () => {
            unlockScroll(dialog);
            // A dialog that closed has handed the focus back already. Blazor releases before it removes the dialog.
            if (!dialog.open) return;
            setTimeout(() => {
                if (!dialog.isConnected && opener?.isConnected) opener.focus();
            });
        },
    };
}

export function closeModal(dialog: HTMLDialogElement) {
    if (dialog.open) dialog.close();
}
