// Formats and highlights the source of a code preview off the page's thread. A message { id, format, language } with
// `format` a text answers { id, formatted } or { id, invalid: true }; a message { id, highlight, language } with
// `highlight` a list of texts answers { id, html } with their escaped, highlighted markup. Any other failure answers
// { id, failed: true }.
import { formatCode, highlightCode } from '../lib/code-preview/code-preview.min.js';

self.onmessage = async ({ data }) => {
    const { id, language } = data;

    try {
        if (data.highlight) {
            self.postMessage({ id, html: data.highlight.map(text => highlightCode(text, language)) });
        } else {
            const formatted = await formatCode(data.format, language);

            self.postMessage(formatted === null ? { id, invalid: true } : { id, formatted });
        }
    } catch {
        self.postMessage({ id, failed: true });
    }
};
