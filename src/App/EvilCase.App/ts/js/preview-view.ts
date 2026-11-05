import { stepZoom, touchZoom, wheelZoom } from './preview-shared.js';
import { debouncedFind, fold } from './text-search.js';
import type { FindEntry } from './text-search.js';

// The size of a view's page.
export interface PageSize {
    width: number;
    height: number;
}

// The folded text of a view's content: each character's text node, by its number in textNodes or -1 between two blocks,
// and its offsets there.
interface TextIndex {
    text: string;
    textNodes: Text[];
    nodes: number[];
    starts: number[];
    ends: number[];
}

interface ViewSearch {
    ranges: Range[];
    current: number;
    painted: boolean;
}

export interface ViewState extends FindEntry {
    element: Element;
    stage: HTMLElement;
    content: HTMLElement;
    root: Node;
    view: DotNetObject;
    page: PageSize | null;
    opening: string;
    abort: AbortController;
    zoom: number;
    fit: string | null;
    index: TextIndex | null;
    search: ViewSearch | null;
    sections: HTMLElement[];
    current: number;
    pinned: number | null;
}

// The blob URLs of each rendered document's pictures, revoked once its element has left the page.
export const documentUrls = new Map<Element, string[]>();

// The previews laid out here rather than by PDF.js: a Markdown, e-mail or data message reflows at its zoom, a document
// keeps its pages and scales them. Each reports its zoom and search matches to `view`, a .NET object reference, and a
// document also the page in view.
export const fitModes = ['page-width', 'page-fit'];
const views = new Map<Element, ViewState>();
const highlights: HighlightRegistry | undefined = globalThis.CSS?.highlights;
const hitName = 'ec-preview-hit';
const currentHitName = 'ec-preview-hit-current';
export const highlightRules = `::highlight(${hitName}) { background-color: var(--ec-preview-hit); }
::highlight(${currentHitName}) { background-color: var(--ec-preview-hit-current); }`;
// Text in two of these never forms one match.
export const blocks = 'address, article, blockquote, dd, div, dl, dt, figcaption, h1, h2, h3, h4, h5, h6, li, ol, p, pre, section, table, td, th, tr, ul';

export function release() {
    for (const [host, urls] of documentUrls) {
        if (!host.isConnected) {
            urls.forEach(url => URL.revokeObjectURL(url));
            documentUrls.delete(host);
        }
    }

    for (const element of views.keys()) {
        if (!element.isConnected) {
            disposeView(element);
        }
    }
}

export function disposeView(element: Element | null) {
    // The element is gone once its file left the page, and the caller then holds no reference to it.
    if (!element) {
        release();

        return;
    }

    const state = views.get(element);

    if (state) {
        views.delete(element);
        state.abort.abort();
        clearSearch(state);
    }
}

// A view with a page scales it rather than reflowing, so two fingers also pan it; `opening` is the fit it opens at.
export function openView(element: Element, stage: HTMLElement, content: HTMLElement, root: Node, view: DotNetObject, page: PageSize | null, opening = 'page-width') {
    disposeView(element);

    const abort = new AbortController();
    const state: ViewState = { element, stage, content, root, view, page, opening, abort, zoom: 1, fit: null, index: null, query: '', search: null, typing: null, sections: [], current: 0, pinned: null };

    views.set(element, state);

    wheelZoom(stage, abort.signal, () => state.zoom, (zoom, x, y) => setZoom(state, zoom, null, x, y));
    touchZoom(stage, abort.signal, () => state.zoom, (zoom, x, y) => setZoom(state, zoom, null, x, y),
        page ? { reset: () => fitPage(state, 'opening'), pan: true } : {});

    return state;
}

// The point of the content at x, y of the stage stays in place.
function setZoom(state: ViewState, zoom: number, fit: string | null, x = 0, y = 0) {
    const { stage } = state;
    const height = stage.scrollHeight;
    const width = stage.scrollWidth;
    const top = stage.scrollTop + y;
    const left = stage.scrollLeft + x;

    state.zoom = zoom;
    state.fit = fit;
    state.content.style.setProperty('--ec-preview-zoom', String(zoom));
    stage.scrollTop = (top * stage.scrollHeight) / Math.max(height, 1) - y;
    stage.scrollLeft = (left * stage.scrollWidth) / Math.max(width, 1) - x;

    const preset = fit === 'opening' ? (zoom < 1 ? state.opening : '') : fit ?? '';

    if (views.get(state.element) === state) {
        state.view.invokeMethodAsync('ZoomChanged', Math.round(zoom * 100), preset).catch(() => {});
    }
}

// The opening zoom is the view's opening fit, at most 100 %; it follows the stage's size until the zoom changes.
export function fitPage(state: ViewState, fit: string) {
    const { stage } = state;
    const page = state.page!;
    const style = getComputedStyle(stage);
    const width = stage.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    const height = stage.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    const toWidth = width / page.width;
    const fitted = (fit === 'opening' ? state.opening : fit) === 'page-fit' ? Math.min(toWidth, height / page.height) : toWidth;
    const zoom = fit === 'opening' ? Math.min(fitted, 1) : fitted;

    if (zoom > 0) {
        setZoom(state, zoom, fit);
    }
}

// The fit follows the stage's size until the zoom changes.
export function followStage(state: ViewState) {
    const resize = new ResizeObserver(() => {
        if (state.fit) {
            fitPage(state, state.fit);
        }
    });

    resize.observe(state.stage);
    state.abort.signal.addEventListener('abort', () => resize.disconnect(), { once: true });
}

// An image zooms by its size, so it stays sharp; 100 % is its own pixels, an image without them keeps the size it
// shows at. Answers whether the image took the zoom.
export function openImageView(stage: HTMLElement, image: HTMLImageElement, view: DotNetObject) {
    release();

    const page = { width: image.naturalWidth || image.offsetWidth, height: image.naturalHeight || image.offsetHeight };

    if (!image.isConnected || page.width === 0 || page.height === 0) {
        return false;
    }

    image.style.setProperty('--ec-preview-image-width', `${page.width}px`);
    image.style.setProperty('--ec-preview-image-height', `${page.height}px`);

    const state = openView(stage, stage, image, image, view, page, 'page-fit');

    followStage(state);
    fitPage(state, 'opening');

    return true;
}

export function openTextView(stage: HTMLElement, view: DotNetObject) {
    release();

    const content = stage.querySelector<HTMLElement>('[role="document"]');

    if (content) {
        openView(stage, stage, content, content, view, null);
    }
}

// The page in view is the one that fills most of the stage; of two that fill it alike, the one shown before, else
// the upper one. A page gone to stays the page in view until the next scroll, even when it cannot reach the top.
function pageInView(state: ViewState) {
    const { sections } = state;
    const view = state.stage.getBoundingClientRect();
    let low = 0;
    let high = sections.length - 1;

    while (low < high) {
        const middle = (low + high) >> 1;

        if (sections[middle].getBoundingClientRect().bottom <= view.top) {
            low = middle + 1;
        } else {
            high = middle;
        }
    }

    let best = low;
    let shown = -1;

    for (let number = low; number < sections.length; number++) {
        const box = sections[number].getBoundingClientRect();

        if (box.top >= view.bottom) {
            break;
        }

        const visible = Math.min(box.bottom, view.bottom) - Math.max(box.top, view.top);

        if (visible > shown + 1 || (visible > shown - 1 && number + 1 === state.current)) {
            best = number;
            shown = visible;
        }
    }

    return best + 1;
}

export function reportPage(state: ViewState) {
    const number = state.pinned ?? pageInView(state);

    state.pinned = null;

    if (number !== state.current && views.get(state.element) === state) {
        state.current = number;
        state.view.invokeMethodAsync('PageChanged', number).catch(() => {});
    }
}

// The top of the page comes to the top of the stage, as far as the stage scrolls.
export function goToViewPage(element: Element, page: number) {
    const state = views.get(element);
    const section = state?.sections[page - 1];

    if (!section) {
        return;
    }

    const { stage } = state!;
    const top = stage.scrollTop;

    stage.scrollTop += section.getBoundingClientRect().top - stage.getBoundingClientRect().top - parseFloat(getComputedStyle(stage).paddingTop);
    state!.pinned = page;

    // Without a scroll there is no scroll event to report the page.
    if (stage.scrollTop === top) {
        reportPage(state!);
    }
}

export function zoomView(element: Element, zoom: string) {
    const state = views.get(element);

    if (!state) {
        return;
    }

    if (fitModes.includes(zoom)) {
        if (state.page) {
            fitPage(state, zoom);
        }

        return;
    }

    setZoom(state, stepZoom(state.zoom, zoom), null);
}

// The folded text of the content, with the text node and offsets of each of its characters.
function indexText(root: Node) {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
        acceptNode: node => (node.parentElement?.closest('style, script, [aria-hidden="true"]') ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT),
    });
    const index: TextIndex = { text: '', textNodes: [], nodes: [], starts: [], ends: [] };
    const parts: string[] = [];
    let block: Element | null | undefined;

    for (let node = walker.nextNode() as Text | null; node; node = walker.nextNode() as Text | null) {
        const owner = node.parentElement?.closest(blocks) ?? null;

        if (index.nodes.length > 0 && owner !== block) {
            parts.push('\n');
            index.nodes.push(-1);
            index.starts.push(0);
            index.ends.push(0);
        }

        block = owner;

        const number = index.textNodes.push(node) - 1;
        const value = node.data;

        for (let offset = 0; offset < value.length;) {
            const size = value.codePointAt(offset)! > 0xffff ? 2 : 1;
            const folded = fold(value.slice(offset, offset + size));

            for (let char = 0; char < folded.length; char++) {
                index.nodes.push(number);
                index.starts.push(offset);
                index.ends.push(offset + size);
            }

            parts.push(folded);
            offset += size;
        }
    }

    index.text = parts.join('');

    return index;
}

function rangeOf(index: TextIndex, start: number, end: number) {
    const range = new Range();

    range.setStart(index.textNodes[index.nodes[start]], index.starts[start]);
    range.setEnd(index.textNodes[index.nodes[end - 1]], index.ends[end - 1]);

    return range;
}

// The views shown side by side share the two highlights; each adds and removes only its own matches.
function sharedHighlight(name: string) {
    if (!highlights!.has(name)) {
        highlights!.set(name, new Highlight());
    }

    return highlights!.get(name)!;
}

function clearSearch(state: ViewState) {
    clearTimeout(state.typing ?? undefined);

    if (state.search?.painted && highlights) {
        for (const range of state.search.ranges) {
            highlights.get(hitName)?.delete(range);
            highlights.get(currentHitName)?.delete(range);
        }
    }

    state.search = null;
}

// The match shown is drawn apart from the rest and scrolled to the middle of the stage when it is out of view.
function showMatch(state: ViewState, number: number) {
    const { stage } = state;
    const search = state.search!;
    const previous = search.ranges[search.current];
    const range = search.ranges[number];

    search.current = number;

    if (highlights) {
        const hits = sharedHighlight(hitName);
        const currentHits = sharedHighlight(currentHitName);

        if (search.painted) {
            hits.add(previous);
            currentHits.delete(previous);
        } else {
            search.ranges.forEach(each => hits.add(each));
            search.painted = true;
        }

        hits.delete(range);
        currentHits.add(range);
    }

    // A code block scrolls sideways on its own, so the match first comes into its view; its scroll offset is
    // measured unzoomed.
    const block = range.startContainer.parentElement?.closest('pre');

    if (block && block.scrollWidth > block.clientWidth) {
        const inner = range.getBoundingClientRect();
        const outer = block.getBoundingClientRect();

        if (inner.left < outer.left || inner.right > outer.right) {
            block.scrollLeft += ((inner.left - outer.left - (outer.width - inner.width) / 2) * block.offsetWidth) / outer.width;
        }
    }

    const box = range.getBoundingClientRect();
    const view = stage.getBoundingClientRect();

    if (box.top < view.top || box.bottom > view.bottom) {
        stage.scrollTop += box.top - view.top - (view.height - box.height) / 2;
    }

    if (box.left < view.left || box.right > view.right) {
        stage.scrollLeft += box.left - view.left - (view.width - box.width) / 2;
    }
}

// The first match of a new query is the first one not above the top of the stage.
function firstInView(state: ViewState) {
    const { ranges } = state.search!;
    const top = state.stage.getBoundingClientRect().top;
    let low = 0;
    let high = ranges.length;

    while (low < high) {
        const middle = (low + high) >> 1;

        if (ranges[middle].getBoundingClientRect().bottom < top) {
            low = middle + 1;
        } else {
            high = middle;
        }
    }

    return low < ranges.length ? low : 0;
}

// A match ignores case and diacritics; the search wraps around.
export function findInView(element: Element, query: string, again: boolean, previous: boolean) {
    const state = views.get(element);

    if (!state) {
        return;
    }

    debouncedFind(state, query, again, {
        clear: () => clearSearch(state),
        search: () => search(state),
        step: () => {
            const { ranges, current } = state.search!;

            if (ranges.length > 0) {
                showMatch(state, (current + (previous ? ranges.length - 1 : 1)) % ranges.length);
            }

            reportMatches(state);
        },
    });
}

function search(state: ViewState) {
    if (!state.index || !state.index.textNodes.every(node => node.isConnected)) {
        state.index = indexText(state.root);
    }

    const { index } = state;
    const needle = fold(state.query);
    const ranges: Range[] = [];

    for (let start = index.text.indexOf(needle); needle.length > 0 && start >= 0; start = index.text.indexOf(needle, start + needle.length)) {
        ranges.push(rangeOf(index, start, start + needle.length));
    }

    clearSearch(state);
    state.search = { ranges, current: 0, painted: false };

    if (ranges.length > 0) {
        showMatch(state, firstInView(state));
    }

    reportMatches(state);
}

function reportMatches(state: ViewState) {
    const { ranges, current } = state.search!;

    state.view.invokeMethodAsync('MatchesChanged', ranges.length > 0 ? current + 1 : 0, ranges.length, false).catch(() => {});
}
