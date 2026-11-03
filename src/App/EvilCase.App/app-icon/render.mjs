// Renders the app icons of the web app manifest and iOS into wwwroot/icons: the PNGs of
// wwwroot/favicon.svg (purpose any) and of its full-bleed variant without the rounded corners
// (maskable, apple-touch-icon), whose scales stay inside the maskable safe zone.
//
// Usage: PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers node render.mjs   (no parameters, any working directory)
// PLAYWRIGHT_MODULE names Playwright's index.mjs; the default is the global install under /opt/node22.
// Overwrites the PNGs; exits 1 when Playwright or its Chromium is missing or a source is unreadable.

import { mkdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const wwwroot = join(here, '..', 'wwwroot');
const out = join(wwwroot, 'icons');
const playwright = process.env.PLAYWRIGHT_MODULE ?? '/opt/node22/lib/node_modules/playwright/index.mjs';

let rounded;
try {
    rounded = readFileSync(join(wwwroot, 'favicon.svg'), 'utf8');
} catch (error) {
    console.error(`render: ${error.message}`);
    process.exit(1);
}
const fullBleed = rounded.replace(/ rx="[^"]*"/, '');
const icons = [
    { source: rounded, size: 192, file: 'icon-192.png' },
    { source: rounded, size: 512, file: 'icon-512.png' },
    { source: fullBleed, size: 512, file: 'icon-maskable-512.png' },
    { source: fullBleed, size: 180, file: 'apple-touch-icon.png' },
];

let chromium;
try {
    ({ chromium } = await import(playwright));
} catch (error) {
    console.error(`render: cannot load Playwright from ${playwright}: ${error.message}`);
    process.exit(1);
}

mkdirSync(out, { recursive: true });
const browser = await chromium.launch().catch(error => {
    console.error(`render: cannot start Chromium: ${error.message}`);
    process.exit(1);
});

try {
    for (const { source, size, file } of icons) {
        const svg = source.replace('<svg ', `<svg width="${size}" height="${size}" `);
        const page = await browser.newPage({ viewport: { width: size, height: size } });
        await page.setContent(`<style>html, body { margin: 0; background: transparent; } svg { display: block; }</style>${svg}`);
        await page.screenshot({ path: join(out, file), omitBackground: true });
        await page.close();
        console.log(`render: ${file}`);
    }
} catch (error) {
    console.error(`render: ${error.message}`);
    process.exitCode = 1;
} finally {
    await browser.close();
}
