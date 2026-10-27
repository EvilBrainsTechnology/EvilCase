// Previews of the formats the browser cannot show itself. Each library under ../lib is loaded the first time
// a file of its type opens; the bytes are parsed here and never leave the browser.

const maxPixels = 64 * 1024 * 1024;
const maxBodyLength = 200000;
const maxColumns = 100;
const safeLink = /^(https?:|mailto:)/i;
const loading = new Map();

function loadScript(path) {
    if (!loading.has(path)) {
        const promise = new Promise((resolve, reject) => {
            const script = document.createElement('script');

            script.src = new URL(`../lib/${path}`, import.meta.url).href;
            script.onload = () => resolve();
            script.onerror = () => {
                loading.delete(path);
                reject(new Error(`The library ${path} did not load.`));
            };
            document.head.appendChild(script);
        });

        loading.set(path, promise);
    }

    return loading.get(path);
}

function extensionOf(fileName) {
    return fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase();
}

// A link in a rendered document opens in a new tab, and only a web or mail address stays a link.
function secureLinks(root) {
    for (const anchor of root.querySelectorAll('a')) {
        const href = anchor.getAttribute('href') ?? '';

        if (safeLink.test(href)) {
            anchor.target = '_blank';
            anchor.rel = 'noopener noreferrer';
        } else {
            anchor.removeAttribute('href');
        }
    }
}

// The blob URLs of each rendered document's pictures, revoked once its element has left the page.
const documentUrls = new Map();

// The previews laid out here rather than by PDF.js: a text, Markdown or e-mail reflows at its zoom, a document keeps
// its pages and scales them. Each reports its zoom and search matches to `view`, a .NET object reference, and a
// document also the page in view.
const zoomSteps = [0.5, 0.67, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4];
const fitModes = ['page-width', 'page-fit'];
const views = new Map();
const highlights = globalThis.CSS?.highlights;
const hitName = 'ec-preview-hit';
const currentHitName = 'ec-preview-hit-current';
const typingDelay = 250;
const highlightRules = `::highlight(${hitName}) { background-color: var(--ec-preview-hit); }
::highlight(${currentHitName}) { background-color: var(--ec-preview-hit-current); }`;
// Text in two of these never forms one match.
const blocks = 'address, article, blockquote, dd, div, dl, dt, figcaption, h1, h2, h3, h4, h5, h6, li, ol, p, pre, section, table, td, th, tr, ul';
let highlighted = null;

function release() {
    for (const [host, urls] of documentUrls) {
        if (!host.isConnected) {
            urls.forEach(url => URL.revokeObjectURL(url));
            documentUrls.delete(host);
        }
    }

    for (const element of views.keys()) {
        if (!element.isConnected) {
            disposeView(element);
        }
    }
}

export function disposeView(element) {
    // The element is gone once its file left the page, and the caller then holds no reference to it.
    if (!element) {
        release();

        return;
    }

    const state = views.get(element);

    if (state) {
        views.delete(element);
        state.abort.abort();
        clearSearch(state);
    }
}

function openView(element, stage, content, root, view, page) {
    disposeView(element);

    const abort = new AbortController();
    const state = { element, stage, content, root, view, page, abort, zoom: 1, fit: null, index: null, search: null, typing: null, sections: [], current: 0, pinned: null };

    views.set(element, state);

    // Ctrl or Cmd with the wheel, and a pinch on a touchpad, zoom around the pointer.
    stage.addEventListener('wheel', event => {
        if (!event.ctrlKey && !event.metaKey) {
            return;
        }

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;
        // A fit outside the steps' range is a bound of its own, so the wheel never zooms against its direction.
        const current = state.zoom;
        const zoom = Math.min(Math.max(current * factor, Math.min(zoomSteps[0], current)), Math.max(zoomSteps.at(-1), current));
        const box = stage.getBoundingClientRect();

        if (zoom !== current) {
            setZoom(state, zoom, null, event.clientX - box.left, event.clientY - box.top);
        }
    }, { passive: false, signal: abort.signal });

    return state;
}

// The point of the content at x, y of the stage stays in place.
function setZoom(state, zoom, fit, x = 0, y = 0) {
    const { stage } = state;
    const height = stage.scrollHeight;
    const width = stage.scrollWidth;
    const top = stage.scrollTop + y;
    const left = stage.scrollLeft + x;

    state.zoom = zoom;
    state.fit = fit;
    state.content.style.setProperty('--ec-preview-zoom', String(zoom));
    stage.scrollTop = (top * stage.scrollHeight) / Math.max(height, 1) - y;
    stage.scrollLeft = (left * stage.scrollWidth) / Math.max(width, 1) - x;

    const preset = fit === 'opening' ? (zoom < 1 ? 'page-width' : '') : fit ?? '';

    if (views.get(state.element) === state) {
        state.view.invokeMethodAsync('ZoomChanged', Math.round(zoom * 100), preset).catch(() => {});
    }
}

// The opening zoom of a document is the page width, at most 100 %; it follows the stage's size until the zoom changes.
function fitPage(state, fit) {
    const { stage, page } = state;
    const style = getComputedStyle(stage);
    const width = stage.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    const height = stage.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    const toWidth = width / page.width;
    const zoom = fit === 'page-fit' ? Math.min(toWidth, height / page.height) : fit === 'opening' ? Math.min(toWidth, 1) : toWidth;

    if (zoom > 0) {
        setZoom(state, zoom, fit);
    }
}

export function openTextView(stage, view) {
    release();

    const content = stage.querySelector('[role="document"]');

    if (content) {
        openView(stage, stage, content, content, view, null);
    }
}

// The page in view is the one that fills most of the stage; of two that fill it alike, the one shown before, else
// the upper one. A page gone to stays the page in view until the next scroll, even when it cannot reach the top.
function pageInView(state) {
    const { sections } = state;
    const view = state.stage.getBoundingClientRect();
    let low = 0;
    let high = sections.length - 1;

    while (low < high) {
        const middle = (low + high) >> 1;

        if (sections[middle].getBoundingClientRect().bottom <= view.top) {
            low = middle + 1;
        } else {
            high = middle;
        }
    }

    let best = low;
    let shown = -1;

    for (let number = low; number < sections.length; number++) {
        const box = sections[number].getBoundingClientRect();

        if (box.top >= view.bottom) {
            break;
        }

        const visible = Math.min(box.bottom, view.bottom) - Math.max(box.top, view.top);

        if (visible > shown + 1 || (visible > shown - 1 && number + 1 === state.current)) {
            best = number;
            shown = visible;
        }
    }

    return best + 1;
}

function reportPage(state) {
    const number = state.pinned ?? pageInView(state);

    state.pinned = null;

    if (number !== state.current && views.get(state.element) === state) {
        state.current = number;
        state.view.invokeMethodAsync('PageChanged', number).catch(() => {});
    }
}

// The library keeps a section that starts on a new page on the page before unless the paper changes, so such a
// section ends with a page break.
function breakSections(parsed) {
    for (const element of parsed.documentPart?.body?.children ?? []) {
        const type = element.sectionProps?.type;

        if (element.sectionProps && type !== 'continuous' && type !== 'nextColumn') {
            element.children = [...(element.children ?? []), { type: 'run', children: [{ type: 'break', break: 'page' }] }];
        }
    }
}

function hasRenderedBreaks(element) {
    return (element.type === 'break' && element.break === 'lastRenderedPageBreak') || (element.children ?? []).some(hasRenderedBreaks);
}

// A page taller than its paper splits between blocks, and a table between rows outside a merged cell; a block taller
// than a page stays whole. The pages that follow take the header and footer of the page split, the notes go to the
// last of them. A document Word laid out splits only a page that overflows by more than `slack`, a fraction of the
// paper, as its pages differ from Word's only in the fonts. The breaks are measured before anything moves.
function paginate(page, slack) {
    const paper = parseFloat(getComputedStyle(page).minHeight);
    const blocks = [...page.querySelectorAll(':scope > article > *')];

    if (!(paper > 0) || blocks.length === 0 || page.offsetHeight <= paper * (1 + slack) + 1) {
        return [page];
    }

    const footer = page.querySelector(':scope > footer');
    const footerSpace = footer ? Math.max(0, footer.offsetHeight + parseFloat(getComputedStyle(footer).marginBottom)) : 0;
    const available = paper - parseFloat(getComputedStyle(page).paddingBottom) - footerSpace - blocks[0].offsetTop;
    const starts = [];
    let pageTop = blocks[0].offsetTop;

    for (const block of blocks) {
        const top = block.offsetTop;

        if (top + block.offsetHeight - pageTop <= available) {
            continue;
        }

        const rows = block.tagName === 'TABLE' ? tableRows(block) : [];

        if (rows.length === 0) {
            if (top > pageTop) {
                starts.push({ block });
                pageTop = top;
            }

            continue;
        }

        rows.forEach((row, number) => {
            const rowTop = top + row.offsetTop;

            if (rowTop + row.offsetHeight - pageTop > available && rowTop > pageTop) {
                if (number === 0) {
                    starts.push({ block });
                    pageTop = rowTop;
                } else if (rowsApart(rows, number)) {
                    starts.push({ block, row: number });
                    pageTop = rowTop;
                }
            }
        });
    }

    // A table splits from its last break back, so the rows before each break stay where they were counted.
    const firsts = new Set(starts.toReversed().map(({ block, row }) => row === undefined ? block : splitTable(block, row)));
    const notes = [...page.querySelectorAll(':scope > ol')];
    const pages = [page];
    let articles = new Map();

    for (const block of [...page.querySelectorAll(':scope > article > *')]) {
        const article = block.parentElement;

        if (firsts.has(block)) {
            const next = page.cloneNode(false);
            const header = page.querySelector(':scope > header');

            if (header) {
                next.append(header.cloneNode(true));
            }

            if (footer) {
                next.append(footer.cloneNode(true));
            }

            pages.at(-1).after(next);
            pages.push(next);
            articles = new Map();
        }

        if (pages.length > 1) {
            const current = pages.at(-1);

            if (!articles.has(article)) {
                articles.set(article, current.insertBefore(article.cloneNode(false), current.querySelector(':scope > footer')));
            }

            articles.get(article).append(block);
        }
    }

    const last = pages.at(-1);

    notes.forEach(note => last.insertBefore(note, last.querySelector(':scope > footer')));

    return pages;
}

function tableRows(table) {
    return [...table.querySelectorAll(':scope > tbody > tr, :scope > tr')];
}

// No cell merged down from an earlier row reaches the row.
function rowsApart(rows, number) {
    return rows.slice(0, number).every((row, index) => [...row.cells].every(cell => index + cell.rowSpan <= number));
}

// The rows from `number` on move to a copy of the table after it, which repeats its columns and header rows.
function splitTable(table, number) {
    const rest = table.cloneNode(false);
    const body = document.createElement('tbody');

    table.querySelectorAll(':scope > colgroup, :scope > thead').forEach(part => rest.append(part.cloneNode(true)));
    rest.append(body);
    body.append(...tableRows(table).slice(number));
    table.after(rest);

    return rest;
}

// A page with nothing on it comes from two breaks in a row: Word marks where its page began right after a page
// break, and the library breaks at both.
function isBlank(section) {
    const article = section.querySelector(':scope > article');

    return article !== null && article.textContent.trim() === '' && article.querySelector('img, svg, table, canvas') === null;
}

// The document renders in a shadow root, so the styles it carries apply to it alone. A page ends where Word ended it
// when it last laid the document out, or at a page or section break. Returns the page count, or 0 when nothing shows.
export async function renderDocument(host, contentStream, view) {
    release();

    const buffer = await contentStream.arrayBuffer();

    await loadScript('jszip/jszip.min.js');
    await loadScript('docx-preview/docx-preview.min.js');

    (documentUrls.get(host) ?? []).forEach(url => URL.revokeObjectURL(url));

    const page = document.createElement('div');
    const root = page.attachShadow({ mode: 'closed' });

    host.replaceChildren(page);

    let wordBreaks = false;

    try {
        const options = { inWrapper: true, breakPages: true, ignoreLastRenderedPageBreak: false, renderAltChunks: false };
        const parsed = await window.docx.parseAsync(buffer, options);

        breakSections(parsed);
        wordBreaks = hasRenderedBreaks(parsed.documentPart?.body ?? {});
        root.append(...await window.docx.renderDocument(parsed, options));
        secureLinks(root);
    } finally {
        documentUrls.set(host, root.innerHTML.match(/blob:[^\s"'()<>]+/g) ?? []);
    }

    const style = document.createElement('style');

    style.textContent = highlightRules;
    root.append(style);
    root.querySelector('.docx-wrapper')?.setAttribute('part', 'document-wrapper');

    // A page is as wide as its paper and at least as tall; its paper size is what a fit fits.
    // The fonts the first layout asks for load before the pages are measured.
    void root.firstElementChild?.offsetHeight;
    await document.fonts?.ready;

    // Without the pages Word laid out, the pages are counted from the paper's height.
    const sections = [...root.querySelectorAll('section.docx')].flatMap(section => paginate(section, wordBreaks ? 0.1 : 0));
    const pages = sections.filter(section => !isBlank(section));

    if (pages.length === 0) {
        pages.push(...sections.slice(0, 1));
    }

    sections.filter(section => !pages.includes(section)).forEach(section => section.remove());
    const size = {
        width: Math.max(0, ...pages.map(section => section.offsetWidth)),
        height: Math.max(0, ...pages.map(section => parseFloat(getComputedStyle(section).minHeight) || section.offsetHeight)),
    };

    pages.forEach(section => section.setAttribute('part', 'document-page'));

    const stage = host.parentElement;

    if (!host.isConnected || !stage || size.width === 0 || size.height === 0) {
        return 0;
    }

    const state = openView(host, stage, host, root, view, size);
    let frame = 0;

    state.sections = pages;
    stage.addEventListener('scroll', () => {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(() => reportPage(state));
    }, { passive: true, signal: state.abort.signal });
    state.abort.signal.addEventListener('abort', () => cancelAnimationFrame(frame), { once: true });
    const resize = new ResizeObserver(() => {
        if (state.fit) {
            fitPage(state, state.fit);
        }
    });

    resize.observe(stage);
    state.abort.signal.addEventListener('abort', () => resize.disconnect(), { once: true });
    fitPage(state, 'opening');
    reportPage(state);

    return pages.length;
}

// The top of the page comes to the top of the stage, as far as the stage scrolls.
export function goToViewPage(element, page) {
    const state = views.get(element);
    const section = state?.sections[page - 1];

    if (!section) {
        return;
    }

    const { stage } = state;
    const top = stage.scrollTop;

    stage.scrollTop += section.getBoundingClientRect().top - stage.getBoundingClientRect().top - parseFloat(getComputedStyle(stage).paddingTop);
    state.pinned = page;

    // Without a scroll there is no scroll event to report the page.
    if (stage.scrollTop === top) {
        reportPage(state);
    }
}

export function zoomView(element, zoom) {
    const state = views.get(element);

    if (!state) {
        return;
    }

    if (fitModes.includes(zoom)) {
        if (state.page) {
            fitPage(state, zoom);
        }

        return;
    }

    const current = state.zoom;
    const next = zoom === 'actual'
        ? 1
        : zoom === 'in'
            ? zoomSteps.find(step => step > current + 0.005) ?? zoomSteps.at(-1)
            : zoomSteps.findLast(step => step < current - 0.005) ?? zoomSteps[0];

    setZoom(state, next, null);
}

// Case, diacritics and the kind of space make no difference to a match.
function fold(text) {
    let folded = '';

    for (const char of text) {
        const code = char.codePointAt(0);

        if (code < 0x80) {
            folded += code >= 65 && code <= 90 ? String.fromCharCode(code + 32) : code === 9 || code === 10 || code === 13 ? ' ' : char;
        } else {
            folded += /\s/.test(char) ? ' ' : char.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
        }
    }

    return folded;
}

// The folded text of the content, with the text node and offsets of each of its characters.
function indexText(root) {
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
        acceptNode: node => (node.parentElement?.closest('style, script, [aria-hidden="true"]') ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT),
    });
    const index = { text: '', textNodes: [], nodes: [], starts: [], ends: [] };
    const parts = [];
    let block;

    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const owner = node.parentElement?.closest(blocks) ?? null;

        if (index.nodes.length > 0 && owner !== block) {
            parts.push('\n');
            index.nodes.push(-1);
            index.starts.push(0);
            index.ends.push(0);
        }

        block = owner;

        const number = index.textNodes.push(node) - 1;
        const value = node.data;

        for (let offset = 0; offset < value.length;) {
            const size = value.codePointAt(offset) > 0xffff ? 2 : 1;
            const folded = fold(value.slice(offset, offset + size));

            for (let char = 0; char < folded.length; char++) {
                index.nodes.push(number);
                index.starts.push(offset);
                index.ends.push(offset + size);
            }

            parts.push(folded);
            offset += size;
        }
    }

    index.text = parts.join('');

    return index;
}

function rangeOf(index, start, end) {
    const range = new Range();

    range.setStart(index.textNodes[index.nodes[start]], index.starts[start]);
    range.setEnd(index.textNodes[index.nodes[end - 1]], index.ends[end - 1]);

    return range;
}

function clearSearch(state) {
    clearTimeout(state.typing);
    state.search = null;

    if (highlighted === state) {
        highlights?.delete(hitName);
        highlights?.delete(currentHitName);
        highlighted = null;
    }
}

// The match shown is drawn apart from the rest and scrolled to the middle of the stage when it is out of view.
function showMatch(state, number) {
    const { search, stage } = state;
    const previous = search.ranges[search.current];
    const range = search.ranges[number];

    search.current = number;

    if (highlights) {
        if (highlighted !== state) {
            highlights.set(hitName, new Highlight());
            highlights.set(currentHitName, new Highlight());
            highlighted = state;
        }

        const hits = highlights.get(hitName);

        if (search.painted) {
            hits.add(previous);
        } else {
            hits.clear();
            search.ranges.forEach(each => hits.add(each));
            search.painted = true;
        }

        hits.delete(range);
        highlights.get(currentHitName).clear();
        highlights.get(currentHitName).add(range);
    }

    // A code block scrolls sideways on its own, so the match first comes into its view; its scroll offset is
    // measured unzoomed.
    const block = range.startContainer.parentElement?.closest('pre');

    if (block && block.scrollWidth > block.clientWidth) {
        const inner = range.getBoundingClientRect();
        const outer = block.getBoundingClientRect();

        if (inner.left < outer.left || inner.right > outer.right) {
            block.scrollLeft += ((inner.left - outer.left - (outer.width - inner.width) / 2) * block.offsetWidth) / outer.width;
        }
    }

    const box = range.getBoundingClientRect();
    const view = stage.getBoundingClientRect();

    if (box.top < view.top || box.bottom > view.bottom) {
        stage.scrollTop += box.top - view.top - (view.height - box.height) / 2;
    }

    if (box.left < view.left || box.right > view.right) {
        stage.scrollLeft += box.left - view.left - (view.width - box.width) / 2;
    }
}

// The first match of a new query is the first one not above the top of the stage.
function firstInView(state) {
    const { ranges } = state.search;
    const top = state.stage.getBoundingClientRect().top;
    let low = 0;
    let high = ranges.length;

    while (low < high) {
        const middle = (low + high) >> 1;

        if (ranges[middle].getBoundingClientRect().bottom < top) {
            low = middle + 1;
        } else {
            high = middle;
        }
    }

    return low < ranges.length ? low : 0;
}

// An empty query clears the highlights at once; a new query waits for the typing to pause, as in PDF.js. A match
// ignores case and diacritics; the search wraps around.
export function findInView(element, query, again, previous) {
    const state = views.get(element);

    if (!state) {
        return;
    }

    clearTimeout(state.typing);

    if (query.length === 0) {
        clearSearch(state);
    } else if (again) {
        search(state, query, true, previous);
    } else {
        state.typing = setTimeout(() => search(state, query, false, false), typingDelay);
    }
}

function search(state, query, again, previous) {
    if (!again || state.search?.query !== query) {
        if (!state.index || !state.index.textNodes.every(node => node.isConnected)) {
            state.index = indexText(state.root);
        }

        const { index } = state;
        const needle = fold(query);
        const ranges = [];

        for (let start = index.text.indexOf(needle); needle.length > 0 && start >= 0; start = index.text.indexOf(needle, start + needle.length)) {
            ranges.push(rangeOf(index, start, start + needle.length));
        }

        clearSearch(state);
        state.search = { query, ranges, current: 0, painted: false };

        if (ranges.length > 0) {
            showMatch(state, firstInView(state));
        }
    } else if (state.search.ranges.length > 0) {
        const { ranges, current } = state.search;

        showMatch(state, (current + (previous ? ranges.length - 1 : 1)) % ranges.length);
    }

    const { ranges, current } = state.search;

    state.view.invokeMethodAsync('MatchesChanged', ranges.length > 0 ? current + 1 : 0, ranges.length, false).catch(() => {});
}

// A JSON, XML, YAML or HTML source shows highlighted in numbered lines, formatted unless it cannot be or was cut; the
// formatted and the original text stay to switch between. Returns whether it formatted and whether lines were cut.
const maxCodeLines = 5000;

function codeLib() {
    return import(new URL('../lib/code-preview/code-preview.min.js', import.meta.url).href);
}

export async function openCodeView(stage, view, text, language, cut) {
    const { formatCode } = await codeLib();
    let formatted = null;

    if (!cut) {
        try {
            formatted = await formatCode(text, language);
        } catch {
            // Not valid in its language: the original shows.
        }
    }

    release();

    const content = stage.querySelector('[role="document"]');

    if (!stage.isConnected || !content) {
        return { formatted: false, linesCut: false };
    }

    const state = openView(stage, stage, content, content, view, null);

    state.code = { text, formatted, language };

    return { formatted: formatted !== null, linesCut: await showCode(stage, formatted !== null) };
}

// Shows the formatted or the original text again and runs an open search over it; returns whether lines were cut.
export async function showCode(stage, formatted) {
    const state = views.get(stage);

    if (!state?.code) {
        return false;
    }

    const { highlightCode } = await codeLib();
    const { code } = state;
    const source = (formatted ? code.formatted : code.text).replace(/\r\n/g, '\n');
    const count = source.split('\n').length - (source.endsWith('\n') ? 1 : 0);
    const template = document.createElement('template');

    // highlight.js escapes all of the text; the template keeps its markup inert.
    template.innerHTML = highlightCode(source, code.language);
    state.content.replaceChildren(...highlightedLines(template.content, maxCodeLines).map((line, index) => {
        const row = document.createElement('div');
        const number = document.createElement('span');
        const text = document.createElement('span');

        row.className = 'ec-preview-line';
        number.className = 'ec-preview-line-number';
        number.setAttribute('aria-hidden', 'true');
        number.textContent = String(index + 1);
        text.className = 'ec-preview-line-text';
        text.append(line);
        row.append(number, text);

        return row;
    }));
    state.index = null;

    if (state.search) {
        search(state, state.search.query, false, false);
    }

    return count > maxCodeLines;
}

// Splits highlighted markup at its line ends into at most `max` lines; a span that crosses a line end closes there
// and opens again on the next line. A final line end starts no line.
function highlightedLines(fragment, max) {
    const lines = [];
    let line = document.createDocumentFragment();
    let open = [];
    let parent = line;

    function nextLine() {
        lines.push(line);
        line = document.createDocumentFragment();
        parent = line;
        open = open.map(span => (parent = parent.appendChild(span.cloneNode(false))));
    }

    function walk(node) {
        for (const child of node.childNodes) {
            if (lines.length >= max) {
                return;
            }

            if (child.nodeType === Node.TEXT_NODE) {
                const parts = child.data.split('\n');

                for (let index = 0; index < parts.length && lines.length < max; index++) {
                    if (index > 0) {
                        nextLine();
                    }

                    if (parts[index]) {
                        parent.append(parts[index]);
                    }
                }
            } else {
                parent = parent.appendChild(child.cloneNode(false));
                open.push(parent);
                walk(child);
                open.pop();
                parent = open.at(-1) ?? line;
            }
        }
    }

    walk(fragment);

    if (lines.length < max && (line.textContent.length > 0 || lines.length === 0)) {
        lines.push(line);
    }

    return lines;
}

// PDF.js shows a PDF in its viewer component: pages drawn as they come near the viewport, a text layer to select
// and search, and links. Forms, scripts and the editor stay off. The document loads from its content URL with the
// session cookie; the view reports its page, zoom and search matches to `view`, a .NET object reference.
// The canvas limit PDF.js sets itself on phones and tablets, whose browsers fail on larger canvases.
const pdfCanvasPixels = /Android|\b(?:iPad|iPhone|iPod)(?=;)/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
    ? 5242880
    : 16 * 1024 * 1024;
const pdfs = new Map();
let pdfjs;
let pdfViewerModule;

function pdfLib(name) {
    return new URL(`../lib/pdfjs/${name}`, import.meta.url).href;
}

async function loadPdfjs() {
    if (!pdfjs) {
        pdfjs = await import(pdfLib('pdf.min.mjs'));
        pdfjs.GlobalWorkerOptions.workerSrc = pdfLib('pdf.worker.min.mjs');
    }

    return pdfjs;
}

// The stylesheet goes before the app's own, so a rule of the app wins over one of the viewer.
function loadStylesheet(href) {
    if (!loading.has(href)) {
        const promise = new Promise((resolve, reject) => {
            const link = document.createElement('link');

            link.rel = 'stylesheet';
            link.href = href;
            link.onload = () => resolve();
            link.onerror = () => {
                loading.delete(href);
                link.remove();
                reject(new Error(`The stylesheet ${href} did not load.`));
            };
            document.head.insertBefore(link, document.head.querySelector('link[rel="stylesheet"]'));
        });

        loading.set(href, promise);
    }

    return loading.get(href);
}

// The library module sets the global pdfjsLib the viewer module reads when it loads.
async function loadPdfViewer() {
    const lib = await loadPdfjs();

    await loadStylesheet(pdfLib('pdf_viewer.css'));
    pdfViewerModule ??= await import(pdfLib('pdf_viewer.mjs'));

    return { lib, viewer: pdfViewerModule };
}

// The viewer's labels; it fetches no locale files when it is given its own.
const czechL10n = {
    getLanguage: () => 'cs',
    getDirection: () => 'ltr',
    get: async ids => (Array.isArray(ids) ? ids.map(() => '') : ''),
    translate: async () => {},
    translateOnce: async () => {},
    destroy: async () => {},
    pause: () => {},
    resume: () => {},
};

export function disposePdf(host) {
    // The element is gone once its file left the page, and the caller then holds no reference to it.
    if (!host) {
        releasePdfs();

        return;
    }

    const pdf = pdfs.get(host);

    if (pdf) {
        pdfs.delete(host);
        pdf.dispose();
    }
}

function releasePdfs() {
    for (const host of pdfs.keys()) {
        if (!host.isConnected) {
            disposePdf(host);
        }
    }
}

export async function renderPdf(host, url, view) {
    return await showPdf(host, { url, withCredentials: true }, view);
}

// An attachment of a data message is held in memory; the library reads its bytes rather than a URL.
export async function renderPdfContent(host, contentStream, view) {
    return await showPdf(host, { data: new Uint8Array(await contentStream.arrayBuffer()) }, view);
}

async function showPdf(host, source, view) {
    releasePdfs();
    disposePdf(host);

    try {
        return await openPdf(host, source, view);
    } catch (error) {
        disposePdf(host);

        throw error;
    }
}

function on(eventBus, name, signal, listener) {
    eventBus.on(name, listener, { signal });
}

function next(eventBus, name, signal) {
    return new Promise(resolve => eventBus.on(name, resolve, { once: true, signal }));
}

async function openPdf(host, source, view) {
    const { lib, viewer: viewerLib } = await loadPdfViewer();

    // A preview closed while the library loaded has nothing left to release it, so nothing is opened for it.
    if (!host.isConnected) {
        return 0;
    }

    const abort = new AbortController();
    const { signal } = abort;
    const eventBus = new viewerLib.EventBus();

    // Only a web or mail address stays a link, and it opens in a new tab; any other address is no link at all.
    const linkService = new (class extends viewerLib.PDFLinkService {
        addLinkAttributes(link, address) {
            if (safeLink.test(address)) {
                super.addLinkAttributes(link, address, true);
            }
        }
    })({ eventBus, externalLinkTarget: viewerLib.LinkTarget.BLANK, externalLinkRel: 'noopener noreferrer nofollow' });
    const findController = new viewerLib.PDFFindController({ eventBus, linkService });
    const pages = document.createElement('div');

    pages.className = 'pdfViewer';
    host.replaceChildren(pages);

    const viewer = new viewerLib.PDFViewer({
        container: host,
        viewer: pages,
        eventBus,
        linkService,
        findController,
        l10n: czechL10n,
        textLayerMode: 1,
        annotationMode: lib.AnnotationMode.ENABLE,
        annotationEditorMode: lib.AnnotationEditorType.DISABLE,
        imageResourcesPath: pdfLib('images/'),
        maxCanvasPixels: pdfCanvasPixels,
        abortSignal: signal,
    });

    linkService.setViewer(viewer);

    const task = lib.getDocument({
        ...source,
        enableXfa: false,
        verbosity: 0,
        wasmUrl: pdfLib('wasm/'),
        standardFontDataUrl: pdfLib('standard_fonts/'),
        iccUrl: pdfLib('iccs/'),
    });
    // The opening zoom is the page width, at most 100 %; it follows the preview's size until the zoom changes.
    const pdf = { disposed: false, ready: false, opening: true, viewer, eventBus, document: null };
    const frame = host.parentElement;

    pdf.dispose = () => {
        pdf.disposed = true;
        abort.abort();
        viewer.setDocument(null);
        linkService.setDocument(null);
        host.replaceChildren();
        // Destroys the document and terminates its worker.
        task.destroy();
    };
    pdfs.set(host, pdf);

    const notify = (method, ...args) => {
        if (!pdf.disposed) {
            view.invokeMethodAsync(method, ...args).catch(() => {});
        }
    };

    on(eventBus, 'pagechanging', signal, ({ pageNumber }) => notify('PageChanged', pageNumber));
    on(eventBus, 'scalechanging', signal, ({ scale, presetValue }) => notify('ZoomChanged', Math.round(scale * 100), presetValue ?? ''));
    on(eventBus, 'updatefindmatchescount', signal, ({ matchesCount }) => notify('MatchesChanged', matchesCount.current, matchesCount.total, false));
    on(eventBus, 'updatefindcontrolstate', signal, ({ state, matchesCount }) =>
        notify('MatchesChanged', matchesCount.current, matchesCount.total, state === viewerLib.FindState.PENDING));

    // A password-protected or damaged file rejects here, and nothing is shown of it.
    pdf.document = await task.promise;

    if (pdf.disposed) {
        return 0;
    }

    const initialized = next(eventBus, 'pagesinit', signal);
    const firstRendered = next(eventBus, 'pagerendered', signal);

    viewer.setDocument(pdf.document);
    linkService.setDocument(pdf.document, null);
    await initialized;

    if (pdf.disposed) {
        return 0;
    }

    const count = pdf.document.numPages;

    for (const page of pages.querySelectorAll('.page[data-page-number]')) {
        page.setAttribute('aria-label', `Strana ${page.dataset.pageNumber} z ${count}`);
    }

    fitOpening(viewer);

    // The first page is drawn before the preview opens, so a file that parses but does not render shows the fallback.
    const { error } = await firstRendered;

    if (error) {
        throw error;
    }

    if (pdf.disposed) {
        return 0;
    }

    pdf.ready = true;

    // A fitted zoom follows the size of the preview; any other zoom only draws the pages that came into view.
    const resize = new ResizeObserver(() => {
        if (pdf.opening) {
            fitOpening(viewer);
        } else if (fitModes.includes(viewer.currentScaleValue)) {
            viewer.currentScaleValue = viewer.currentScaleValue;
        } else {
            viewer.update();
        }
    });

    resize.observe(frame);
    signal.addEventListener('abort', () => resize.disconnect(), { once: true });

    // Ctrl or Cmd with the wheel, and a pinch on a touchpad, zoom around the pointer.
    host.addEventListener('wheel', event => {
        if (!event.ctrlKey && !event.metaKey) {
            return;
        }

        event.preventDefault();

        const fine = event.deltaMode === WheelEvent.DOM_DELTA_PIXEL && Math.abs(event.deltaY) < 50;
        const factor = fine ? Math.exp(-event.deltaY / 100) : event.deltaY < 0 ? 1.1 : 1 / 1.1;
        // A fit outside the steps' range is a bound of its own, so the wheel never zooms against its direction.
        const current = viewer.currentScale;
        const scale = Math.min(Math.max(current * factor, Math.min(zoomSteps[0], current)), Math.max(zoomSteps.at(-1), current));
        const box = host.getBoundingClientRect();

        if (scale === current) {
            return;
        }

        pdf.opening = false;
        viewer.updateScale({ scaleFactor: scale / current, origin: [event.clientX - box.left, event.clientY - box.top] });
    }, { passive: false, signal });

    return count;
}

function fitOpening(viewer) {
    viewer.currentScaleValue = 'page-width';

    if (viewer.currentScale > 1) {
        viewer.currentScale = 1;
    }
}

export function zoomPdf(host, zoom) {
    const pdf = pdfs.get(host);

    if (!pdf?.ready) {
        return;
    }

    pdf.opening = false;

    if (fitModes.includes(zoom)) {
        pdf.viewer.currentScaleValue = zoom;

        return;
    }

    if (zoom === 'actual') {
        pdf.viewer.currentScale = 1;

        return;
    }

    const current = pdf.viewer.currentScale;
    const scale = zoom === 'in'
        ? zoomSteps.find(step => step > current + 0.005) ?? zoomSteps.at(-1)
        : zoomSteps.findLast(step => step < current - 0.005) ?? zoomSteps[0];

    pdf.viewer.currentScale = scale;
}

export function goToPdfPage(host, page) {
    const pdf = pdfs.get(host);

    if (pdf?.ready) {
        pdf.viewer.currentPageNumber = Math.min(Math.max(page, 1), pdf.viewer.pagesCount);
    }
}

// An empty query clears the highlights. Matches ignore case and diacritics.
export function findInPdf(host, query, again, previous) {
    const pdf = pdfs.get(host);

    if (!pdf?.ready) {
        return;
    }

    if (query.length === 0) {
        pdf.eventBus.dispatch('findbarclose', { source: null });

        return;
    }

    pdf.eventBus.dispatch('find', {
        source: null,
        type: again ? 'again' : '',
        query,
        caseSensitive: false,
        entireWord: false,
        highlightAll: true,
        findPrevious: previous,
        matchDiacritics: false,
    });
}

export async function readWorkbook(contentStream, maxRows) {
    release();

    const buffer = await contentStream.arrayBuffer();

    // SheetJS also reads text and HTML as a sheet; only a ZIP or an OLE container is a workbook here.
    const head = new Uint8Array(buffer, 0, Math.min(4, buffer.byteLength));
    const isZip = head[0] === 0x50 && head[1] === 0x4b;
    const isOle = head[0] === 0xd0 && head[1] === 0xcf && head[2] === 0x11 && head[3] === 0xe0;

    if (!isZip && !isOle) {
        throw new Error('The file is not a workbook.');
    }

    await loadScript('sheetjs/xlsx.full.min.js');

    const XLSX = window.XLSX;
    const workbook = XLSX.read(buffer, { type: 'array', dense: true, cellDates: true, dateNF: 'd. m. yyyy' });
    const sheets = [];

    for (const name of workbook.SheetNames) {
        const sheet = workbook.Sheets[name];
        const ref = sheet['!ref'];

        if (!ref) {
            sheets.push({ name, rows: [], rowCount: 0, columnCount: 0 });
            continue;
        }

        // The table starts at A1 whatever the used range says, so the row and column headings match Excel.
        const range = XLSX.utils.decode_range(ref);
        const rowCount = range.e.r + 1;
        const columnCount = Math.min(range.e.c + 1, maxColumns);

        range.s = { r: 0, c: 0 };
        range.e.r = Math.min(range.e.r, maxRows - 1);
        range.e.c = columnCount - 1;

        const rows = XLSX.utils.sheet_to_json(sheet, { header: 1, raw: false, defval: '', blankrows: true, range, dateNF: 'd. m. yyyy' });

        sheets.push({ name, rows: rows.map(row => Array.from({ length: columnCount }, (_, index) => String(row[index] ?? ''))), rowCount, columnCount });
    }

    return sheets;
}

// The text of an HTML body: the page is parsed in an inert document, so nothing in it runs or loads.
function htmlToText(html) {
    const document = new DOMParser().parseFromString(html, 'text/html');

    for (const element of document.querySelectorAll('script, style, head')) {
        element.remove();
    }

    for (const element of document.querySelectorAll('br')) {
        element.replaceWith('\n');
    }

    for (const element of document.querySelectorAll('p, div, tr, li, h1, h2, h3, h4, h5, h6, blockquote, pre')) {
        element.append('\n');
    }

    return document.body.textContent.replace(/\n{3,}/g, '\n\n').trim();
}

function formatAddress(name, address) {
    if (name && address && name !== address) {
        return `${name} <${address}>`;
    }

    return address || name || '';
}

function toIsoDate(value) {
    const date = value ? new Date(value) : null;

    return date && !Number.isNaN(date.getTime()) ? date.toISOString() : null;
}

function emailResult(from, to, cc, subject, date, text, html, attachments) {
    const body = text || (html ? htmlToText(html) : '');

    return {
        from,
        to,
        cc,
        subject: subject || '',
        date: toIsoDate(date),
        body: body.slice(0, maxBodyLength),
        truncated: body.length > maxBodyLength,
        attachments,
    };
}

async function readEml(buffer) {
    const PostalMime = (await import(new URL('../lib/postal-mime/postal-mime.min.js', import.meta.url).href)).default;
    const mail = await PostalMime.parse(buffer);
    const flatten = list => (list ?? []).flatMap(entry => entry.group ?? [entry]).map(entry => formatAddress(entry.name, entry.address));
    const from = mail.from ? formatAddress(mail.from.name, mail.from.address) : '';
    const attachments = (mail.attachments ?? []).map(attachment => ({ name: attachment.filename || 'příloha', size: attachment.content?.byteLength ?? 0 }));

    return emailResult(from, flatten(mail.to), flatten(mail.cc), mail.subject, mail.date, mail.text, mail.html, attachments);
}

async function readMsg(buffer) {
    await loadScript('msgreader/msgreader.min.js');

    const data = new window.EcMsg.MsgReader(buffer).getFileData();

    if (data.error) {
        throw new Error(data.error);
    }

    const recipients = type => (data.recipients ?? []).filter(recipient => (recipient.recipType ?? 'to') === type).map(recipient => formatAddress(recipient.name, recipient.email || recipient.smtpAddress));
    const attachments = (data.attachments ?? []).map(attachment => ({ name: attachment.fileName || attachment.fileNameShort || 'příloha', size: attachment.contentLength ?? 0 }));
    const date = data.messageDeliveryTime || data.clientSubmitTime || data.creationTime;

    return emailResult(formatAddress(data.senderName, data.senderEmail), recipients('to'), recipients('cc'), data.subject, date, data.body, data.bodyHtml, attachments);
}

export async function readEmail(fileName, contentStream) {
    release();

    const buffer = await contentStream.arrayBuffer();

    return extensionOf(fileName) === 'msg' ? readMsg(buffer) : readEml(buffer);
}

async function decodeHeic(buffer) {
    await loadScript('libheif/libheif-bundle.js');

    const libheif = await window.libheif();
    const decoder = new libheif.HeifDecoder();
    const image = decoder.decode(new Uint8Array(buffer))[0];

    if (!image) {
        throw new Error('The file holds no image.');
    }

    const width = image.get_width();
    const height = image.get_height();

    if (width * height > maxPixels) {
        throw new Error('The image is too large.');
    }

    const target = new ImageData(width, height);

    await new Promise((resolve, reject) => {
        image.display(target, result => (result ? resolve() : reject(new Error('The image did not decode.'))));
    });

    return target;
}

// The picture the browser cannot show becomes a PNG blob; the caller revokes the URL.
export async function createPngUrl(contentStream) {
    release();

    const buffer = await contentStream.arrayBuffer();
    const image = await decodeHeic(buffer);
    const canvas = document.createElement('canvas');

    canvas.width = image.width;
    canvas.height = image.height;
    canvas.getContext('2d').putImageData(image, 0, 0);

    const blob = await new Promise((resolve, reject) => canvas.toBlob(result => (result ? resolve(result) : reject(new Error('The image did not encode.'))), 'image/png'));

    return URL.createObjectURL(blob);
}
