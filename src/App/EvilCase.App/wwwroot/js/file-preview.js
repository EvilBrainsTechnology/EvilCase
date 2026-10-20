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

// PDF.js draws the pages of a PDF onto canvases; no text, annotation or form layer is created, so nothing in the
// file is selectable, clickable or scripted. The document loads from its content URL with the session cookie.
const maxPixelRatio = 2;
const maxCanvasPixels = 16 * 1024 * 1024;
const pdfs = new Map();
let pdfjs;

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

// Pages draw as they come near the viewport and are freed once far away, so a long file holds few canvases.
async function drawPage(pdf, page) {
    if (page.task || page.drawn) {
        return;
    }

    const ratio = Math.min(window.devicePixelRatio || 1, maxPixelRatio);
    const cssWidth = page.box.clientWidth;
    let scale = (cssWidth / page.width) * ratio;

    scale = Math.min(scale, Math.sqrt(maxCanvasPixels / (page.width * page.height)));

    const proxy = await pdf.document.getPage(page.number);
    const viewport = proxy.getViewport({ scale });
    const canvas = document.createElement('canvas');

    canvas.width = Math.floor(viewport.width);
    canvas.height = Math.floor(viewport.height);
    page.task = proxy.render({ canvasContext: canvas.getContext('2d'), viewport });

    try {
        await page.task.promise;

        if (!pdf.disposed) {
            page.box.replaceChildren(canvas);
            page.drawn = true;
        }
    } catch (error) {
        if (error?.name !== 'RenderingCancelledException') {
            throw error;
        }
    } finally {
        page.task = null;
        proxy.cleanup();
    }
}

function freePage(page) {
    page.task?.cancel();
    page.drawn = false;
    page.box.replaceChildren();
}

export async function renderPdf(host, url) {
    releasePdfs();
    disposePdf(host);

    try {
        return await openPdf(host, url);
    } catch (error) {
        disposePdf(host);

        throw error;
    }
}

async function openPdf(host, url) {
    const lib = await loadPdfjs();
    const task = lib.getDocument({
        url,
        withCredentials: true,
        enableXfa: false,
        verbosity: 0,
        wasmUrl: pdfLib('wasm/'),
        standardFontDataUrl: pdfLib('standard_fonts/'),
        iccUrl: pdfLib('iccs/'),
    });
    const pdf = { disposed: false, list: [], document: null };
    const observers = [];

    pdf.dispose = () => {
        pdf.disposed = true;
        observers.forEach(observer => observer.disconnect());
        pdf.list.forEach(page => page.task?.cancel());
        host.replaceChildren();
        task.destroy();
    };
    pdfs.set(host, pdf);

    // A password-protected or damaged file rejects here, and nothing is shown of it.
    pdf.document = await task.promise;

    const column = document.createDocumentFragment();

    for (let number = 1; number <= pdf.document.numPages; number++) {
        const proxy = await pdf.document.getPage(number);
        const { width, height } = proxy.getViewport({ scale: 1 });
        const box = document.createElement('div');

        box.className = 'ec-preview-pdf-page';
        box.style.aspectRatio = `${width} / ${height}`;
        box.setAttribute('role', 'img');
        box.setAttribute('aria-label', `Strana ${number} z ${pdf.document.numPages}`);
        proxy.cleanup();
        pdf.list.push({ number, width, height, box, drawn: false, task: null });
        column.appendChild(box);

        if (pdf.disposed) {
            return 0;
        }
    }

    host.replaceChildren(column);

    const byBox = new Map(pdf.list.map(page => [page.box, page]));
    const near = new IntersectionObserver(entries => {
        for (const entry of entries) {
            const page = byBox.get(entry.target);

            if (entry.isIntersecting) {
                drawPage(pdf, page).catch(error => console.warn('A PDF page did not render.', error));
            } else {
                freePage(page);
            }
        }
    }, { root: host.parentElement, rootMargin: '150% 0px' });

    observers.push(near);

    // The first page is drawn before the preview opens, so a file that parses but does not render shows the fallback.
    await drawPage(pdf, pdf.list[0]);

    pdf.list.forEach(page => near.observe(page.box));

    let lastWidth = host.clientWidth;
    const resize = new ResizeObserver(() => {
        if (host.clientWidth !== lastWidth) {
            lastWidth = host.clientWidth;
            pdf.list.forEach(freePage);
            // Re-observing reports every page again, which draws the ones in view.
            pdf.list.forEach(page => {
                near.unobserve(page.box);
                near.observe(page.box);
            });
        }
    });

    resize.observe(host);
    observers.push(resize);

    return pdf.document.numPages;
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

async function decodeTiff(buffer) {
    await loadScript('pako/pako.min.js');
    await loadScript('utif/UTIF.min.js');

    const UTIF = window.UTIF;
    const pages = UTIF.decode(buffer);

    if (pages.length === 0) {
        throw new Error('The file holds no image.');
    }

    const page = pages[0];

    // Width and height tags, checked before the pixels are decoded.
    if ((page.t256?.[0] ?? 0) * (page.t257?.[0] ?? 0) > maxPixels) {
        throw new Error('The image is too large.');
    }

    UTIF.decodeImage(buffer, page);

    return new ImageData(new Uint8ClampedArray(UTIF.toRGBA8(page)), page.width, page.height);
}

// The picture the browser cannot show becomes a PNG blob; the caller revokes the URL.
export async function createPngUrl(fileName, contentStream) {
    releaseDocuments();

    const buffer = await contentStream.arrayBuffer();
    const image = ['tif', 'tiff'].includes(extensionOf(fileName)) ? await decodeTiff(buffer) : await decodeHeic(buffer);
    const canvas = document.createElement('canvas');

    canvas.width = image.width;
    canvas.height = image.height;
    canvas.getContext('2d').putImageData(image, 0, 0);

    const blob = await new Promise((resolve, reject) => canvas.toBlob(result => (result ? resolve(result) : reject(new Error('The image did not encode.'))), 'image/png'));

    return URL.createObjectURL(blob);
}
