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

// On desktop a double-click on the header of a dialog marked data-ec-maximizable, outside its controls, toggles its size.
const desktop = matchMedia('(min-width: 992px)');

function toggleSize(event) {
    const dialog = event.currentTarget;
    if (!dialog.hasAttribute('data-ec-maximizable') || !desktop.matches) return;
    if (!event.target.closest('.ec-modal-header')) return;
    if (event.target.closest('a, button, input, select, textarea, [contenteditable], .ec-file-rename, .ec-menu-anchor, .ec-popover-anchor')) return;
    // The double-click selected a word of the title.
    window.getSelection()?.removeAllRanges();
    owners.get(dialog)?.invokeMethodAsync('ToggleMaximized');
}

// The page does not scroll while any dialog is open; the last one to close returns it to where it stood.
const locks = new Set();
let page = null;

function lockScroll(dialog) {
    if (locks.size === 0) {
        page = { href: location.href, x: window.scrollX, y: window.scrollY };
        document.documentElement.classList.add('ec-scroll-locked');
    }

    locks.add(dialog);
}

function unlockScroll(dialog) {
    if (!locks.delete(dialog) || locks.size > 0) return;

    document.documentElement.classList.remove('ec-scroll-locked');

    if (location.href === page.href && (window.scrollX !== page.x || window.scrollY !== page.y)) window.scrollTo(page.x, page.y);
}

// Answers the dialog's lock of the page scroll; its release frees a dialog removed while open, which fires no close,
// and hands the focus back to the element that held it before the dialog opened, as a close does.
export function openModal(dialog, owner) {
    let opener = null;

    if (!dialog.open) {
        opener = document.activeElement;
        owners.set(dialog, owner);
        dialog.addEventListener('cancel', requestClose);
        dialog.addEventListener('keydown', keepEscape);
        dialog.addEventListener('dblclick', toggleSize);
        dialog.addEventListener('close', () => {
            unlockScroll(dialog);
            owner.invokeMethodAsync('NotifyClosed');
        }, { once: true });
        dialog.showModal();
        dialog.querySelector('input:not([type=file]), select, textarea, button')?.focus();
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

export function closeModal(dialog) {
    if (dialog.open) dialog.close();
}
