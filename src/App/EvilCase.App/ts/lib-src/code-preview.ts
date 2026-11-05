// Formats JSON, XML, YAML and HTML. `json-stringify` keeps every JSON number literal and escape as
// written; XML keeps its whitespace; HTML leaves its embedded scripts and styles as they are.

import { format } from 'prettier/standalone';
import * as babel from 'prettier/plugins/babel';
import * as estree from 'prettier/plugins/estree';
import * as html from 'prettier/plugins/html';
import * as yaml from 'prettier/plugins/yaml';
import xml from '@prettier/plugin-xml';

const options = {
    json: { parser: 'json-stringify' },
    xml: { parser: 'xml', xmlWhitespaceSensitivity: 'preserve' },
    yaml: { parser: 'yaml' },
    html: { parser: 'html', embeddedLanguageFormatting: 'off' },
} as const;

// Returns the formatted text, or null when the text is not valid in its language; any other failure throws.
// json-stringify also accepts JSON5, so JSON is first checked by JSON.parse.
export async function formatCode(text: string, language: keyof typeof options) {
    try {
        if (language === 'json') {
            JSON.parse(text);
        }

        return await format(text, { ...options[language], plugins: [babel, estree, html, yaml, xml] });
    } catch (error) {
        if (error instanceof SyntaxError) {
            return null;
        }

        throw error;
    }
}
