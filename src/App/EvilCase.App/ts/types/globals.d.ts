// The globals of the libraries loadScript loads as classic scripts; each is there once its script has loaded.
interface Window {
    docx: typeof import('docx-preview');
    libheif: () => Promise<LibHeif>;
}

// The part of libheif-js the HEIC preview uses.
interface LibHeif {
    HeifDecoder: new () => {
        decode(data: Uint8Array): LibHeifImage[];
    };
}

interface LibHeifImage {
    get_width(): number;
    get_height(): number;
    display(target: ImageData, callback: (result: ImageData | null) => void): void;
}
