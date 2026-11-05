// The zoom, the stylesheets and the scripts the previews, the text view and the Markdown editor share.

const zoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4];

const doubleTapMs = 300;

// A lone finger that lifts this close to where it touched is a tap.
const tapSlop = 10;

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
// zoom other than current() with the pointer's position in element.
export function wheelZoom(element, signal, current, apply) {
    element.addEventListener('wheel', (event) => {
        if (!event.ctrlKey && !event.metaKey) return;

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;
        const zoom = current();
        const next = bounded(zoom * factor, zoom);
        const box = element.getBoundingClientRect();

        if (next !== zoom) apply(next, event.clientX - box.left, event.clientY - box.top);
    }, { passive: false, signal });
}

// The zoom a pinch from start leads to when the fingers moved from startDistance apart to distance apart.
export function pinchZoom(start, startDistance, distance) {
    return bounded((start * distance) / startDistance, start);
}

// Two fingers zoom the scrolling element until signal aborts: during the gesture its content scales around the fingers'
// midpoint, at its end apply(zoom, x, y) hears the zoom with the midpoint in element. With pan the content also follows
// the midpoint, and after apply the element scrolls the point first under the fingers to where they left it. A double
// tap calls reset, when there is one.
export function touchZoom(element, signal, current, apply, { reset = null, pan = false } = {}) {
    let gesture = null;
    let touched = null;
    let tapped = -Infinity;

    const spread = (touches) => Math.hypot(touches[0].clientX - touches[1].clientX, touches[0].clientY - touches[1].clientY);
    const midpoint = (touches) => ({ x: (touches[0].clientX + touches[1].clientX) / 2, y: (touches[0].clientY + touches[1].clientY) / 2 });

    element.addEventListener('touchstart', (event) => {
        touched = event.touches.length === 1 ? { x: event.touches[0].clientX, y: event.touches[0].clientY } : null;

        if (event.touches.length !== 2) return;

        const surface = [...element.children].find((child) => child.getClientRects().length > 0) ?? element;
        const box = element.getBoundingClientRect();
        const inner = surface.getBoundingClientRect();
        const start = midpoint(event.touches);

        gesture = {
            surface,
            zoom: current(),
            distance: spread(event.touches),
            next: current(),
            start,
            mid: start,
            left: element.scrollLeft,
            top: element.scrollTop,
            x: start.x - box.left,
            y: start.y - box.top,
        };
        surface.style.transformOrigin = `${start.x - inner.left}px ${start.y - inner.top}px`;
    }, { passive: true, signal });

    element.addEventListener('touchmove', (event) => {
        if (!gesture || event.touches.length !== 2) return;

        event.preventDefault();
        gesture.next = pinchZoom(gesture.zoom, gesture.distance, spread(event.touches));
        gesture.mid = midpoint(event.touches);

        // A scroll the browser began before the second finger touched goes on and moves the content as well.
        const x = gesture.mid.x - gesture.start.x + element.scrollLeft - gesture.left;
        const y = gesture.mid.y - gesture.start.y + element.scrollTop - gesture.top;
        const scale = `scale(${gesture.next / gesture.zoom})`;

        gesture.surface.style.transform = pan ? `translate(${x}px, ${y}px) ${scale}` : scale;
    }, { passive: false, signal });

    const end = (event) => {
        if (!gesture || event.touches.length === 2) return;

        const { surface, zoom, next, start, mid } = gesture;
        const scrolled = { x: element.scrollLeft - gesture.left, y: element.scrollTop - gesture.top };
        const x = pan ? gesture.x - scrolled.x : gesture.x;
        const y = pan ? gesture.y - scrolled.y : gesture.y;

        gesture = null;
        surface.style.transform = '';
        surface.style.transformOrigin = '';

        if (next !== zoom) apply(next, x, y);

        if (pan) {
            element.scrollLeft -= mid.x - start.x + scrolled.x;
            element.scrollTop -= mid.y - start.y + scrolled.y;
        }
    };

    element.addEventListener('touchend', (event) => {
        end(event);

        if (!reset || event.touches.length > 0) return;

        const touch = event.changedTouches[0];
        const tap = touched !== null && Math.hypot(touch.clientX - touched.x, touch.clientY - touched.y) <= tapSlop;
        const double = tap && event.timeStamp - tapped < doubleTapMs;

        touched = null;
        tapped = tap && !double ? event.timeStamp : -Infinity;

        if (double) {
            event.preventDefault();
            reset();
        }
    }, { signal });

    element.addEventListener('touchcancel', (event) => {
        touched = null;
        end(event);
    }, { signal });
}

// A fit outside the steps' range is a bound of its own, so a zoom never moves against its direction.
function bounded(zoom, current) {
    return Math.min(Math.max(zoom, Math.min(zoomSteps[0], current)), Math.max(zoomSteps.at(-1), current));
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

// A link stays a link only to a web or e-mail address.
export const safeLink = /^(https?:|mailto:)/i;

const loading = new Map();

export function loadScript(path) {
    if (!loading.has(path)) {
        const promise = new Promise((resolve, reject) => {
            const script = document.createElement('script');

            script.src = new URL(`../lib/${path}`, import.meta.url).href;
            script.onload = () => resolve();
            script.onerror = () => {
                loading.delete(path);
                reject(new Error(`The library ${path} did not load.`));
            };
            document.head.appendChild(script);
        });

        loading.set(path, promise);
    }

    return loading.get(path);
}
