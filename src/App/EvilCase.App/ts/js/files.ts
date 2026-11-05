// An ordinary request: the session cookie authenticates it and the download attribute makes the browser
// save the response even when the server sends it inline. An empty name keeps the one the server gives.
export function downloadFile(url: string, fileName: string) {
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
}

// A preview the app renders itself from fetched bytes is shown through a Blob URL.
export async function createObjectUrl(mediaType: string, contentStream: DotNetStream) {
    const buffer = await contentStream.arrayBuffer();

    return URL.createObjectURL(new Blob([buffer], { type: mediaType }));
}

export function revokeObjectUrl(url: string) {
    URL.revokeObjectURL(url);
}

// Content the app holds in memory is saved through a Blob URL of an opaque type, so the browser never shows it.
export async function downloadContent(fileName: string, contentStream: DotNetStream) {
    const url = URL.createObjectURL(new Blob([await contentStream.arrayBuffer()], { type: 'application/octet-stream' }));

    downloadFile(url, fileName);
    // The download has taken the URL long before.
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

// A menu item cannot be the label of a file input, so it opens the input's picker itself.
export function pickFile(input: HTMLInputElement) {
    input.click();
}

// The card is the drop zone (SDD-012): a drop or a paste hands its files to the card's first file
// input, so a dropped and a picked file take one path. The listeners go with the card's element.
export function attachDropZone(card: HTMLElement) {
    let leaving = -1;

    const enter = (event: DragEvent) => {
        event.preventDefault();
        clearTimeout(leaving);
        card.classList.add('hover');
    };

    card.addEventListener('dragenter', enter);
    card.addEventListener('dragover', enter);
    // Passing onto a child leaves the card first; the enter that follows keeps the hover.
    card.addEventListener('dragleave', event => {
        event.preventDefault();
        leaving = setTimeout(() => card.classList.remove('hover'), 10);
    });
    card.addEventListener('drop', event => {
        event.preventDefault();
        clearTimeout(leaving);
        card.classList.remove('hover');
        upload(card, event.dataTransfer!.files);
    });
    card.addEventListener('paste', event => upload(card, event.clipboardData!.files));
}

function upload(card: HTMLElement, files: FileList) {
    if (files.length === 0)
        return;

    const input = card.querySelector<HTMLInputElement>('input[type=file]')!;
    input.files = files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
}
