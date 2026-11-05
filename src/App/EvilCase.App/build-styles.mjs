// Bundles the app stylesheets of wwwroot/css, in the order wwwroot/css/ec.css imports them, into one minified
// wwwroot/lib/ec.min.css with its source map. The bundle sits as deep as wwwroot/css, so the url() paths into
// wwwroot/fonts and wwwroot/img stay as written.
//
// Usage: node build-styles.mjs        (after `npm ci`; no parameters, any working directory)
// Replaces wwwroot/lib/ec.min.css and its map; wwwroot/lib is gitignored.
// Exits 1 when esbuild is not installed or a stylesheet fails to parse.

import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));

if (!existsSync(join(root, 'node_modules', 'esbuild'))) {
    console.error('build-styles: esbuild is not installed; run npm ci first.');
    process.exit(1);
}

const { build } = await import('esbuild');

try {
    await build({
        entryPoints: [join(root, 'wwwroot', 'css', 'ec.css')],
        outfile: join(root, 'wwwroot', 'lib', 'ec.min.css'),
        bundle: true,
        minify: true,
        sourcemap: 'linked',
        // The oldest browsers that have everything the stylesheets use; nesting is lowered for them.
        target: ['chrome105', 'edge105', 'firefox121', 'safari16', 'ios16'],
        external: ['../fonts/*', '../img/*'],
        logLevel: 'warning',
        absWorkingDir: root,
    });
} catch {
    process.exit(1);
}
