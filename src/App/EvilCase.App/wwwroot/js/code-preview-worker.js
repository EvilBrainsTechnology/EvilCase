// Formats the source of a code view off the page's thread: a message { text, language } answers { formatted },
// { invalid: true } when the text is not valid in its language, or { failed: true } on any other failure.
import { formatCode } from '../lib/code-preview/code-preview.min.js';

self.onmessage = async ({ data }) => {
    try {
        const formatted = await formatCode(data.text, data.language);

        self.postMessage(formatted === null ? { invalid: true } : { formatted });
    } catch {
        self.postMessage({ failed: true });
    }
};
