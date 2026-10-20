// An ordinary request: the session cookie authenticates it and the download attribute makes the browser
// save the response even when the server sends it inline.
export function downloadFile(url, fileName) {
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
}

// A preview the app renders itself from fetched bytes is shown through a Blob URL.
export async function createObjectUrl(mediaType, contentStream) {
    const buffer = await contentStream.arrayBuffer();

    return URL.createObjectURL(new Blob([buffer], { type: mediaType }));
}

export function revokeObjectUrl(url) {
    URL.revokeObjectURL(url);
}
