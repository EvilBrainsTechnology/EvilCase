// The zoom and the stylesheets the previews, the text view and the Markdown editor share.

const zoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4];

const stylesheets = new Map();

// The zoom after current for direction in, out or actual; any other direction keeps current.
export function stepZoom(current, direction) {
    return direction === 'actual'
        ? 1
        : direction === 'in'
            ? zoomSteps.find((step) => step > current + 0.005) ?? zoomSteps.at(-1)
            : direction === 'out'
                ? zoomSteps.findLast((step) => step < current - 0.005) ?? zoomSteps[0]
                : current;
}

// Ctrl or Cmd with the wheel, and a pinch on a touchpad, zoom element until signal aborts: apply(zoom, x, y) hears a
// zoom other than current() with the pointer's position in element. A fit outside the steps' range is a bound of its
// own, so the wheel never zooms against its direction.
export function wheelZoom(element, signal, current, apply) {
    element.addEventListener('wheel', (event) => {
        if (!event.ctrlKey && !event.metaKey) return;

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;
        const zoom = current();
        const next = Math.min(Math.max(zoom * factor, Math.min(zoomSteps[0], zoom)), Math.max(zoomSteps.at(-1), zoom));
        const box = element.getBoundingClientRect();

        if (next !== zoom) apply(next, event.clientX - box.left, event.clientY - box.top);
    }, { passive: false, signal });
}

// The stylesheet goes before the app's own, so a rule of the app wins over one of the library.
export function loadStylesheet(href) {
    if (!stylesheets.has(href)) {
        stylesheets.set(href, new Promise((resolve, reject) => {
            const link = document.createElement('link');

            link.rel = 'stylesheet';
            link.href = href;
            link.onload = () => resolve();
            link.onerror = () => {
                stylesheets.delete(href);
                link.remove();
                reject(new Error(`The stylesheet ${href} did not load.`));
            };
            document.head.insertBefore(link, document.head.querySelector('link[rel="stylesheet"]'));
        }));
    }

    return stylesheets.get(href);
}
