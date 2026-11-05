// The workbooks, e-mails and pictures read here for a preview drawn in .NET.
import { loadScript } from './preview-shared.js';
import { release } from './preview-view.js';

const maxPixels = 64 * 1024 * 1024;
const maxColumns = 100;

interface Sheet {
    name: string;
    rows: string[][];
    rowCount: number;
    columnCount: number;
}

export async function readWorkbook(contentStream: DotNetStream, maxRows: number) {
    release();

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
    const sheets: Sheet[] = [];

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

        const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1, raw: false, defval: '', blankrows: true, range, dateNF: 'd. m. yyyy' });

        sheets.push({ name, rows: rows.map(row => Array.from({ length: columnCount }, (_, index) => String(row[index] ?? ''))), rowCount, columnCount });
    }

    return sheets;
}

// The text of an HTML body: the page is parsed in an inert document, so nothing in it runs or loads.
export function htmlToText(html: string) {
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

async function decodeHeic(buffer: ArrayBuffer) {
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

    await new Promise<void>((resolve, reject) => {
        image.display(target, result => (result ? resolve() : reject(new Error('The image did not decode.'))));
    });

    return target;
}

// The picture the browser cannot show becomes a PNG blob; the caller revokes the URL.
export async function createPngUrl(contentStream: DotNetStream) {
    release();

    const buffer = await contentStream.arrayBuffer();
    const image = await decodeHeic(buffer);
    const canvas = document.createElement('canvas');

    canvas.width = image.width;
    canvas.height = image.height;
    canvas.getContext('2d')!.putImageData(image, 0, 0);

    const blob = await new Promise<Blob>((resolve, reject) => canvas.toBlob(result => (result ? resolve(result) : reject(new Error('The image did not encode.'))), 'image/png'));

    return URL.createObjectURL(blob);
}
