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

// Answers the dialog's lock of the page scroll; its release frees a dialog removed while open, which fires no close.
export function openModal(dialog, owner) {
    if (!dialog.open) {
        owners.set(dialog, owner);
        dialog.addEventListener('cancel', requestClose);
        dialog.addEventListener('keydown', keepEscape);
        dialog.addEventListener('close', () => {
            unlockScroll(dialog);
            owner.invokeMethodAsync('NotifyClosed');
        }, { once: true });
        dialog.showModal();
        dialog.querySelector('input:not([type=file]), select, textarea, button')?.focus();
    }

    lockScroll(dialog);

    return { release: () => unlockScroll(dialog) };
}

export function closeModal(dialog) {
    if (dialog.open) dialog.close();
}
