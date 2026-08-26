// The bytes are already fetched with the authorization header, which an anchor cannot send; they become
// a Blob URL and the anchor clicks that.
export async function downloadBlob(fileName, mediaType, contentStream) {
    const buffer = await contentStream.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: mediaType }));
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
}
