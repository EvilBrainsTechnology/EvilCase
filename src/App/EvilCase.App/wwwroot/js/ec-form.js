// An open dialog holds the form being submitted; otherwise the page does.
export function focusFirstInvalid() {
    const root = document.querySelector('dialog[open]') ?? document;
    root.querySelector('[aria-invalid="true"]')?.focus();
}
