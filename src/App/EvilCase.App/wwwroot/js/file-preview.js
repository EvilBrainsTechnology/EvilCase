// Previews of the formats the browser cannot show itself. Each library under ../lib is loaded the first time
// a file of its type opens; the bytes are parsed here and never leave the browser.

const maxPixels = 64 * 1024 * 1024;
const maxBodyLength = 200000;
const maxColumns = 100;
const safeLink = /^(https?:|mailto:)/i;
const loading = new Map();

function loadScript(path) {
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

function extensionOf(fileName) {
    return fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase();
}

// A link in a rendered document opens in a new tab, and only a web or mail address stays a link.
function secureLinks(root) {
    for (const anchor of root.querySelectorAll('a')) {
        const href = anchor.getAttribute('href') ?? '';

        if (safeLink.test(href)) {
            anchor.target = '_blank';
            anchor.rel = 'noopener noreferrer';
        } else {
            anchor.removeAttribute('href');
        }
    }
}

// The blob URLs of each rendered document's pictures, revoked once its element has left the page.
const documentUrls = new Map();

function releaseDocuments() {
    for (const [host, urls] of documentUrls) {
        if (!host.isConnected) {
            urls.forEach(url => URL.revokeObjectURL(url));
            documentUrls.delete(host);
        }
    }

    for (const [host, observer] of documentObservers) {
        if (!host.isConnected) {
            observer.disconnect();
            documentObservers.delete(host);
        }
    }
}

// The observers that keep a rendered document scaled to the width it has.
const documentObservers = new Map();

// The pages shrink to the width of the stage and never grow past their own.
function fitDocument(host, root) {
    documentObservers.get(host)?.disconnect();

    const pages = [...root.querySelectorAll('section.docx')];

    root.querySelector('.docx-wrapper')?.setAttribute('part', 'document-wrapper');
    pages.forEach(page => page.setAttribute('part', 'document-page'));

    const naturalWidth = Math.max(0, ...pages.map(page => page.offsetWidth));
    const stage = host.parentElement;

    if (naturalWidth === 0 || !stage) {
        return;
    }

    const fit = () => {
        const style = getComputedStyle(stage);
        const available = stage.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);

        host.style.setProperty('--ec-document-scale', String(Math.min(1, available / naturalWidth)));
    };

    const observer = new ResizeObserver(fit);

    observer.observe(stage);
    documentObservers.set(host, observer);
    fit();
}

// The document renders in a shadow root, so the styles it carries apply to it alone.
export async function renderDocument(host, contentStream) {
    releaseDocuments();

    const buffer = await contentStream.arrayBuffer();

    await loadScript('jszip/jszip.min.js');
    await loadScript('docx-preview/docx-preview.min.js');

    (documentUrls.get(host) ?? []).forEach(url => URL.revokeObjectURL(url));

    const page = document.createElement('div');
    const root = page.attachShadow({ mode: 'closed' });

    host.replaceChildren(page);

    try {
        await window.docx.renderAsync(buffer, root, null, {
            inWrapper: true,
            breakPages: true,
            ignoreLastRenderedPageBreak: true,
            renderAltChunks: false,
        });
        secureLinks(root);
        fitDocument(host, root);
    } finally {
        documentUrls.set(host, root.innerHTML.match(/blob:[^\s"'()<>]+/g) ?? []);
    }
}

// PDF.js shows a PDF in its viewer component: pages drawn as they come near the viewport, a text layer to select
// and search, and links. Forms, scripts and the editor stay off. The document loads from its content URL with the
// session cookie; the view reports its page, zoom and search matches to `view`, a .NET object reference.
const pdfZoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4];
const pdfFitModes = ['page-width', 'page-fit'];
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

// The stylesheet goes before the app's own, so a rule of the app wins over one of the viewer.
function loadStylesheet(href) {
    if (!loading.has(href)) {
        const promise = new Promise((resolve, reject) => {
            const link = document.createElement('link');

            link.rel = 'stylesheet';
            link.href = href;
            link.onload = () => resolve();
            link.onerror = () => {
                loading.delete(href);
                link.remove();
                reject(new Error(`The stylesheet ${href} did not load.`));
            };
            document.head.insertBefore(link, document.head.querySelector('link[rel="stylesheet"]'));
        });

        loading.set(href, promise);
    }

    return loading.get(href);
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
    releasePdfs();
    disposePdf(host);

    try {
        return await openPdf(host, url, view);
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

async function openPdf(host, url, view) {
    const { lib, viewer: viewerLib } = await loadPdfViewer();
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
        url,
        withCredentials: true,
        enableXfa: false,
        verbosity: 0,
        wasmUrl: pdfLib('wasm/'),
        standardFontDataUrl: pdfLib('standard_fonts/'),
        iccUrl: pdfLib('iccs/'),
    });
    const pdf = { disposed: false, ready: false, viewer, eventBus, document: null };
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

    viewer.currentScaleValue = 'page-width';

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
        if (pdfFitModes.includes(viewer.currentScaleValue)) {
            viewer.currentScaleValue = viewer.currentScaleValue;
        } else {
            viewer.update();
        }
    });

    resize.observe(frame);
    signal.addEventListener('abort', () => resize.disconnect(), { once: true });

    // Ctrl or Cmd with the wheel, and a pinch on a touchpad, zoom around the pointer.
    host.addEventListener('wheel', event => {
        if (!event.ctrlKey && !event.metaKey) {
            return;
        }

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;
        const scale = Math.min(Math.max(viewer.currentScale * factor, pdfZoomSteps[0]), pdfZoomSteps.at(-1));
        const box = host.getBoundingClientRect();

        viewer.updateScale({ scaleFactor: scale / viewer.currentScale, origin: [event.clientX - box.left, event.clientY - box.top] });
    }, { passive: false, signal });

    return count;
}

export function zoomPdf(host, zoom) {
    const pdf = pdfs.get(host);

    if (!pdf?.ready) {
        return;
    }

    if (pdfFitModes.includes(zoom)) {
        pdf.viewer.currentScaleValue = zoom;

        return;
    }

    const current = pdf.viewer.currentScale;
    const scale = zoom === 'in'
        ? pdfZoomSteps.find(step => step > current + 0.005) ?? pdfZoomSteps.at(-1)
        : pdfZoomSteps.findLast(step => step < current - 0.005) ?? pdfZoomSteps[0];

    pdf.viewer.currentScale = scale;
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

export async function readWorkbook(contentStream, maxRows) {
    releaseDocuments();

    const buffer = await contentStream.arrayBuffer();

    // SheetJS also reads text and HTML as a sheet; only a ZIP or an OLE container is a workbook here.
    const head = new Uint8Array(buffer, 0, Math.min(4, buffer.byteLength));
    const isZip = head[0] === 0x50 && head[1] === 0x4b;
    const isOle = head[0] === 0xd0 && head[1] === 0xcf && head[2] === 0x11 && head[3] === 0xe0;

    if (!isZip && !isOle) {
        throw new Error('The file is not a workbook.');
    }

    await loadScript('sheetjs/xlsx.full.min.js');

    const XLSX = window.XLSX;
    const workbook = XLSX.read(buffer, { type: 'array', dense: true, cellDates: true, dateNF: 'd. m. yyyy' });
    const sheets = [];

    for (const name of workbook.SheetNames) {
        const sheet = workbook.Sheets[name];
        const ref = sheet['!ref'];

        if (!ref) {
            sheets.push({ name, rows: [], rowCount: 0, columnCount: 0 });
            continue;
        }

        // The table starts at A1 whatever the used range says, so the row and column headings match Excel.
        const range = XLSX.utils.decode_range(ref);
        const rowCount = range.e.r + 1;
        const columnCount = Math.min(range.e.c + 1, maxColumns);

        range.s = { r: 0, c: 0 };
        range.e.r = Math.min(range.e.r, maxRows - 1);
        range.e.c = columnCount - 1;

        const rows = XLSX.utils.sheet_to_json(sheet, { header: 1, raw: false, defval: '', blankrows: true, range, dateNF: 'd. m. yyyy' });

        sheets.push({ name, rows: rows.map(row => Array.from({ length: columnCount }, (_, index) => String(row[index] ?? ''))), rowCount, columnCount });
    }

    return sheets;
}

// The text of an HTML body: the page is parsed in an inert document, so nothing in it runs or loads.
function htmlToText(html) {
    const document = new DOMParser().parseFromString(html, 'text/html');

    for (const element of document.querySelectorAll('script, style, head')) {
        element.remove();
    }

    for (const element of document.querySelectorAll('br')) {
        element.replaceWith('\n');
    }

    for (const element of document.querySelectorAll('p, div, tr, li, h1, h2, h3, h4, h5, h6, blockquote, pre')) {
        element.append('\n');
    }

    return document.body.textContent.replace(/\n{3,}/g, '\n\n').trim();
}

function formatAddress(name, address) {
    if (name && address && name !== address) {
        return `${name} <${address}>`;
    }

    return address || name || '';
}

function toIsoDate(value) {
    const date = value ? new Date(value) : null;

    return date && !Number.isNaN(date.getTime()) ? date.toISOString() : null;
}

function emailResult(from, to, cc, subject, date, text, html, attachments) {
    const body = text || (html ? htmlToText(html) : '');

    return {
        from,
        to,
        cc,
        subject: subject || '',
        date: toIsoDate(date),
        body: body.slice(0, maxBodyLength),
        truncated: body.length > maxBodyLength,
        attachments,
    };
}

async function readEml(buffer) {
    const PostalMime = (await import(new URL('../lib/postal-mime/postal-mime.min.js', import.meta.url).href)).default;
    const mail = await PostalMime.parse(buffer);
    const flatten = list => (list ?? []).flatMap(entry => entry.group ?? [entry]).map(entry => formatAddress(entry.name, entry.address));
    const from = mail.from ? formatAddress(mail.from.name, mail.from.address) : '';
    const attachments = (mail.attachments ?? []).map(attachment => ({ name: attachment.filename || 'příloha', size: attachment.content?.byteLength ?? 0 }));

    return emailResult(from, flatten(mail.to), flatten(mail.cc), mail.subject, mail.date, mail.text, mail.html, attachments);
}

async function readMsg(buffer) {
    await loadScript('msgreader/msgreader.min.js');

    const data = new window.EcMsg.MsgReader(buffer).getFileData();

    if (data.error) {
        throw new Error(data.error);
    }

    const recipients = type => (data.recipients ?? []).filter(recipient => (recipient.recipType ?? 'to') === type).map(recipient => formatAddress(recipient.name, recipient.email || recipient.smtpAddress));
    const attachments = (data.attachments ?? []).map(attachment => ({ name: attachment.fileName || attachment.fileNameShort || 'příloha', size: attachment.contentLength ?? 0 }));
    const date = data.messageDeliveryTime || data.clientSubmitTime || data.creationTime;

    return emailResult(formatAddress(data.senderName, data.senderEmail), recipients('to'), recipients('cc'), data.subject, date, data.body, data.bodyHtml, attachments);
}

export async function readEmail(fileName, contentStream) {
    releaseDocuments();

    const buffer = await contentStream.arrayBuffer();

    return extensionOf(fileName) === 'msg' ? readMsg(buffer) : readEml(buffer);
}

async function decodeHeic(buffer) {
    await loadScript('libheif/libheif-bundle.js');

    const libheif = await window.libheif();
    const decoder = new libheif.HeifDecoder();
    const image = decoder.decode(new Uint8Array(buffer))[0];

    if (!image) {
        throw new Error('The file holds no image.');
    }

    const width = image.get_width();
    const height = image.get_height();

    if (width * height > maxPixels) {
        throw new Error('The image is too large.');
    }

    const target = new ImageData(width, height);

    await new Promise((resolve, reject) => {
        image.display(target, result => (result ? resolve() : reject(new Error('The image did not decode.'))));
    });

    return target;
}

// The picture the browser cannot show becomes a PNG blob; the caller revokes the URL.
export async function createPngUrl(contentStream) {
    releaseDocuments();

    const buffer = await contentStream.arrayBuffer();
    const image = await decodeHeic(buffer);
    const canvas = document.createElement('canvas');

    canvas.width = image.width;
    canvas.height = image.height;
    canvas.getContext('2d').putImageData(image, 0, 0);

    const blob = await new Promise((resolve, reject) => canvas.toBlob(result => (result ? resolve(result) : reject(new Error('The image did not encode.'))), 'image/png'));

    return URL.createObjectURL(blob);
}
