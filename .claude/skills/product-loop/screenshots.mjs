// Screenshots for visual proof: one browser, one sign-in per width, every screen in one pass.
// Exits non-zero if a page threw — a screenshot of a broken screen is worse than none.
//
//   node .claude/skills/product-loop/screenshots.mjs /tmp/shots/192 targets.json
//
// targets.json is a list of { name, path, file, wait?, steps?, fullPage? }; each step is
// { click?, fill?: [selector, value], wait? }. The width is appended to file: case-list-1440.png.
import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import { readFile, mkdir } from 'node:fs/promises';

const [outDir, targetsPath] = process.argv.slice(2);
if (!outDir || !targetsPath) {
    console.error('usage: screenshots.mjs <out-dir> <targets.json>');
    process.exit(2);
}

const targets = JSON.parse(await readFile(targetsPath, 'utf8'));
const widths = [[1440, 900], [390, 844]];
const origin = process.env.EVILCASE_URL ?? 'https://localhost:5000';
const email = process.env.EVILCASE_EMAIL ?? 'admin@evilcase.local';
const password = process.env.EVILCASE_PASSWORD ?? 'DevPassword123!';

await mkdir(outDir, { recursive: true });

const browser = await chromium.launch();
const broken = [];
const noted = [];

for (const [width, height] of widths) {
    const context = await browser.newContext({
        viewport: { width, height },
        ignoreHTTPSErrors: true,
        deviceScaleFactor: 2,
    });
    const page = await context.newPage();

    // An uncaught exception is fatal: that is how a component that fails to render shows up, and
    // it renders an empty card rather than an error. A console error is only noted — the sign-in
    // page probes for a refresh token and a 401 there is the normal answer.
    let screen = 'sign-in';
    page.on('pageerror', error => broken.push(`${screen} @${width}: ${error.message}`));
    page.on('console', message => {
        if (message.type() === 'error') noted.push(`${screen} @${width}: ${message.text()}`);
    });

    // Once per width. The first WebAssembly load is the slow one; every screen after it is warm.
    await page.goto(`${origin}/`, { waitUntil: 'networkidle' });
    await page.getByLabel(/e-?mail/i).fill(email);
    await page.getByLabel(/heslo|password/i).fill(password);
    await page.getByRole('button', { name: /přihlás|sign in|login/i }).click();
    await page.waitForURL(url => !url.pathname.startsWith('/login'), { timeout: 60_000 });
    await page.waitForLoadState('networkidle');

    for (const target of targets) {
        screen = target.name;
        await page.goto(origin + target.path, { waitUntil: 'networkidle' });
        await page.waitForTimeout(target.wait ?? 1_000);

        for (const step of target.steps ?? []) {
            if (step.click) await page.click(step.click);
            if (step.fill) await page.fill(step.fill[0], step.fill[1]);
            await page.waitForTimeout(step.wait ?? 700);
        }

        const file = `${outDir}/${target.file}-${width}.png`;
        await page.screenshot({ path: file, fullPage: target.fullPage ?? false });
        console.log(`saved ${file}`);
    }

    await context.close();
}

await browser.close();

for (const entry of noted) console.log(`  note: ${entry}`);

if (broken.length > 0) {
    console.error(`\n${broken.length} uncaught exception(s) — these screenshots show a broken screen:`);
    for (const entry of broken) console.error(`  ${entry}`);
    process.exit(1);
}
console.log(`\n${targets.length * widths.length} screenshots, nothing threw`);
