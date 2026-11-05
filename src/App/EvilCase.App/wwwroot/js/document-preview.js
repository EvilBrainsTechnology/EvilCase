// A DOCX document: rendered by docx-preview in a shadow root and split into pages.
import { loadScript, safeLink } from './preview-shared.js';
import { blocks, documentUrls, fitPage, followStage, highlightRules, openView, release, reportPage } from './preview-view.js';

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
    followStage(state);
    fitPage(state, 'opening');
    reportPage(state);

    return pages.length;
}
