// The e-mails and pictures read here for a preview drawn in .NET.
import { loadScript } from './preview-shared.js';
import { release } from './preview-view.js';
import type { Address, Mailbox } from 'postal-mime';

const maxPixels = 64 * 1024 * 1024;
const maxBodyLength = 200000;

interface Attachment {
    name: string;
    size: number;
}

function extensionOf(fileName: string) {
    return fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase();
}

// The text of an HTML body: the page is parsed in an inert document, so nothing in it runs or loads.
function htmlToText(html: string) {
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

function formatAddress(name: string | undefined, address: string | undefined) {
    if (name && address && name !== address) {
        return `${name} <${address}>`;
    }

    return address || name || '';
}

function toIsoDate(value: string | undefined) {
    const date = value ? new Date(value) : null;

    return date && !Number.isNaN(date.getTime()) ? date.toISOString() : null;
}

function emailResult(from: string, to: string[], cc: string[], subject: string | undefined, date: string | undefined, text: string | undefined, html: string | undefined, attachments: Attachment[]) {
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

async function readEml(buffer: ArrayBuffer) {
    const PostalMime = ((await import(new URL('../lib/postal-mime/postal-mime.min.js', import.meta.url).href)) as typeof import('../lib-src/postal-mime.js')).default;
    const mail = await PostalMime.parse(buffer);
    const flatten = (list: Address[] | undefined) => (list ?? []).flatMap((entry): Mailbox[] => entry.group ?? [entry as Mailbox]).map(entry => formatAddress(entry.name, entry.address));
    const from = mail.from ? formatAddress(mail.from.name, mail.from.address) : '';
    const attachments = (mail.attachments ?? []).map(attachment => ({ name: attachment.filename || 'příloha', size: (attachment.content as ArrayBuffer | undefined)?.byteLength ?? 0 }));

    return emailResult(from, flatten(mail.to), flatten(mail.cc), mail.subject, mail.date, mail.text, mail.html, attachments);
}

async function readMsg(buffer: ArrayBuffer) {
    await loadScript('msgreader/msgreader.min.js');

    const data = new window.EcMsg.MsgReader(buffer).getFileData();

    if (data.error) {
        throw new Error(data.error);
    }

    const recipients = (type: string) => (data.recipients ?? []).filter(recipient => (recipient.recipType ?? 'to') === type).map(recipient => formatAddress(recipient.name, recipient.email || recipient.smtpAddress));
    const attachments = (data.attachments ?? []).map(attachment => ({ name: attachment.fileName || attachment.fileNameShort || 'příloha', size: attachment.contentLength ?? 0 }));
    const date = data.messageDeliveryTime || data.clientSubmitTime || data.creationTime;

    return emailResult(formatAddress(data.senderName, data.senderEmail), recipients('to'), recipients('cc'), data.subject, date, data.body, data.bodyHtml, attachments);
}

export async function readEmail(fileName: string, contentStream: DotNetStream) {
    release();

    const buffer = await contentStream.arrayBuffer();

    return extensionOf(fileName) === 'msg' ? readMsg(buffer) : readEml(buffer);
}

// The extension of the type the content's signature shows, or null; plain text has none.
export async function detectExtension(contentStream: DotNetStream) {
    const { fileTypeFromBuffer } = (await import(new URL('../lib/file-type/file-type.min.js', import.meta.url).href)) as typeof import('../lib-src/file-type.js');
    const type = await fileTypeFromBuffer(new Uint8Array(await contentStream.arrayBuffer()));

    return type?.ext ?? null;
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
