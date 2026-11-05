import { loadStylesheet, safeLink, stepZoom, touchZoom, wheelZoom } from './preview-shared.js';
import { fitModes } from './preview-view.js';

// PDF.js shows a PDF in its viewer component: pages drawn as they come near the viewport, a text layer to select
// and search, and links. Forms, scripts and the editor stay off. The document loads from its content URL with the
// session cookie; the view reports its page, zoom and search matches to `view`, a .NET object reference.
// The canvas limit PDF.js sets itself on phones and tablets, whose browsers fail on larger canvases.
const pdfCanvasPixels = /Android|\b(?:iPad|iPhone|iPod)(?=;)/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
    ? 5242880
    : 16 * 1024 * 1024;
const pdfs = new Map();
let pdfjs;
let pdfViewerModule;

function pdfLib(name) {
    return new URL(`../lib/pdfjs/${name}`, import.meta.url).href;
}

async function loadPdfjs() {
    if (!pdfjs) {
        pdfjs = await import(pdfLib('pdf.min.mjs'));
        pdfjs.GlobalWorkerOptions.workerSrc = pdfLib('pdf.worker.min.mjs');
    }

    return pdfjs;
}

// The library module sets the global pdfjsLib the viewer module reads when it loads.
async function loadPdfViewer() {
    const lib = await loadPdfjs();

    await loadStylesheet(pdfLib('pdf_viewer.css'));
    pdfViewerModule ??= await import(pdfLib('pdf_viewer.mjs'));

    return { lib, viewer: pdfViewerModule };
}

// The viewer's labels; it fetches no locale files when it is given its own.
const czechL10n = {
    getLanguage: () => 'cs',
    getDirection: () => 'ltr',
    get: async ids => (Array.isArray(ids) ? ids.map(() => '') : ''),
    translate: async () => {},
    translateOnce: async () => {},
    destroy: async () => {},
    pause: () => {},
    resume: () => {},
};

export function disposePdf(host) {
    // The element is gone once its file left the page, and the caller then holds no reference to it.
    if (!host) {
        releasePdfs();

        return;
    }

    const pdf = pdfs.get(host);

    if (pdf) {
        pdfs.delete(host);
        pdf.dispose();
    }
}

function releasePdfs() {
    for (const host of pdfs.keys()) {
        if (!host.isConnected) {
            disposePdf(host);
        }
    }
}

export async function renderPdf(host, url, view) {
    return await showPdf(host, { url, withCredentials: true }, view);
}

// An attachment of a data message is held in memory; the library reads its bytes rather than a URL.
export async function renderPdfContent(host, contentStream, view) {
    return await showPdf(host, { data: new Uint8Array(await contentStream.arrayBuffer()) }, view);
}

async function showPdf(host, source, view) {
    releasePdfs();
    disposePdf(host);

    try {
        return await openPdf(host, source, view);
    } catch (error) {
        disposePdf(host);

        throw error;
    }
}

function on(eventBus, name, signal, listener) {
    eventBus.on(name, listener, { signal });
}

function next(eventBus, name, signal) {
    return new Promise(resolve => eventBus.on(name, resolve, { once: true, signal }));
}

async function openPdf(host, source, view) {
    const { lib, viewer: viewerLib } = await loadPdfViewer();

    // A preview closed while the library loaded has nothing left to release it, so nothing is opened for it.
    if (!host.isConnected) {
        return 0;
    }

    const abort = new AbortController();
    const { signal } = abort;
    const eventBus = new viewerLib.EventBus();

    // Only a web or mail address stays a link, and it opens in a new tab; any other address is no link at all.
    const linkService = new (class extends viewerLib.PDFLinkService {
        addLinkAttributes(link, address) {
            if (safeLink.test(address)) {
                super.addLinkAttributes(link, address, true);
            }
        }
    })({ eventBus, externalLinkTarget: viewerLib.LinkTarget.BLANK, externalLinkRel: 'noopener noreferrer nofollow' });
    const findController = new viewerLib.PDFFindController({ eventBus, linkService });
    const pages = document.createElement('div');

    pages.className = 'pdfViewer';
    host.replaceChildren(pages);

    const viewer = new viewerLib.PDFViewer({
        container: host,
        viewer: pages,
        eventBus,
        linkService,
        findController,
        l10n: czechL10n,
        textLayerMode: 1,
        annotationMode: lib.AnnotationMode.ENABLE,
        annotationEditorMode: lib.AnnotationEditorType.DISABLE,
        imageResourcesPath: pdfLib('images/'),
        maxCanvasPixels: pdfCanvasPixels,
        abortSignal: signal,
    });

    linkService.setViewer(viewer);

    const task = lib.getDocument({
        ...source,
        enableXfa: false,
        verbosity: 0,
        wasmUrl: pdfLib('wasm/'),
        standardFontDataUrl: pdfLib('standard_fonts/'),
        iccUrl: pdfLib('iccs/'),
    });
    // The opening zoom is the page width, at most 100 %; it follows the preview's size until the zoom changes.
    const pdf = { disposed: false, ready: false, opening: true, viewer, eventBus, document: null };
    const frame = host.parentElement;

    pdf.dispose = () => {
        pdf.disposed = true;
        abort.abort();
        viewer.setDocument(null);
        linkService.setDocument(null);
        host.replaceChildren();
        // Destroys the document and terminates its worker.
        task.destroy();
    };
    pdfs.set(host, pdf);

    const notify = (method, ...args) => {
        if (!pdf.disposed) {
            view.invokeMethodAsync(method, ...args).catch(() => {});
        }
    };

    on(eventBus, 'pagechanging', signal, ({ pageNumber }) => notify('PageChanged', pageNumber));
    on(eventBus, 'scalechanging', signal, ({ scale, presetValue }) => notify('ZoomChanged', Math.round(scale * 100), presetValue ?? ''));
    on(eventBus, 'updatefindmatchescount', signal, ({ matchesCount }) => notify('MatchesChanged', matchesCount.current, matchesCount.total, false));
    on(eventBus, 'updatefindcontrolstate', signal, ({ state, matchesCount }) =>
        notify('MatchesChanged', matchesCount.current, matchesCount.total, state === viewerLib.FindState.PENDING));

    // A password-protected or damaged file rejects here, and nothing is shown of it.
    pdf.document = await task.promise;

    if (pdf.disposed) {
        return 0;
    }

    const initialized = next(eventBus, 'pagesinit', signal);
    const firstRendered = next(eventBus, 'pagerendered', signal);

    viewer.setDocument(pdf.document);
    linkService.setDocument(pdf.document, null);
    await initialized;

    if (pdf.disposed) {
        return 0;
    }

    const count = pdf.document.numPages;

    for (const page of pages.querySelectorAll('.page[data-page-number]')) {
        page.setAttribute('aria-label', `Strana ${page.dataset.pageNumber} z ${count}`);
    }

    fitOpening(viewer);

    // The first page is drawn before the preview opens, so a file that parses but does not render shows the fallback.
    const { error } = await firstRendered;

    if (error) {
        throw error;
    }

    if (pdf.disposed) {
        return 0;
    }

    pdf.ready = true;

    // A fitted zoom follows the size of the preview; any other zoom only draws the pages that came into view.
    const resize = new ResizeObserver(() => {
        if (pdf.opening) {
            fitOpening(viewer);
        } else if (fitModes.includes(viewer.currentScaleValue)) {
            viewer.currentScaleValue = viewer.currentScaleValue;
        } else {
            viewer.update();
        }
    });

    resize.observe(frame);
    signal.addEventListener('abort', () => resize.disconnect(), { once: true });

    const scaleTo = (scale, x, y) => {
        pdf.opening = false;
        viewer.updateScale({ scaleFactor: scale / viewer.currentScale, origin: [x, y] });
    };

    wheelZoom(host, signal, () => viewer.currentScale, scaleTo);
    touchZoom(host, signal, () => viewer.currentScale, scaleTo, {
        reset: () => {
            pdf.opening = true;
            fitOpening(viewer);
        },
        pan: true,
    });

    return count;
}

function fitOpening(viewer) {
    viewer.currentScaleValue = 'page-width';

    if (viewer.currentScale > 1) {
        viewer.currentScale = 1;
    }
}

export function zoomPdf(host, zoom) {
    const pdf = pdfs.get(host);

    if (!pdf?.ready) {
        return;
    }

    pdf.opening = false;

    if (fitModes.includes(zoom)) {
        pdf.viewer.currentScaleValue = zoom;

        return;
    }

    pdf.viewer.currentScale = stepZoom(pdf.viewer.currentScale, zoom);
}

export function goToPdfPage(host, page) {
    const pdf = pdfs.get(host);

    if (pdf?.ready) {
        pdf.viewer.currentPageNumber = Math.min(Math.max(page, 1), pdf.viewer.pagesCount);
    }
}

// An empty query clears the highlights. Matches ignore case and diacritics.
export function findInPdf(host, query, again, previous) {
    const pdf = pdfs.get(host);

    if (!pdf?.ready) {
        return;
    }

    if (query.length === 0) {
        pdf.eventBus.dispatch('findbarclose', { source: null });

        return;
    }

    pdf.eventBus.dispatch('find', {
        source: null,
        type: again ? 'again' : '',
        query,
        caseSensitive: false,
        entireWord: false,
        highlightAll: true,
        findPrevious: previous,
        matchDiacritics: false,
    });
}
