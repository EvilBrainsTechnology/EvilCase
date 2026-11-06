// The compare of two Markdown texts, in one column or side by side.
import { editorViewCtx, parserCtx } from '@milkdown/kit/core';
import { Plugin, PluginKey } from '@milkdown/kit/prose/state';
import { Decoration, DecorationSet } from '@milkdown/kit/prose/view';
import { DOMSerializer, Fragment } from '@milkdown/kit/prose/model';
import { markdownEditor, matchesIn, reveal } from './shared.js';
import { propertiesEnd, showProperties } from './properties.js';
import type { Editor } from '@milkdown/kit/core';
import type { Node as ProseNode } from '@milkdown/kit/prose/model';
import type { EditorView } from '@milkdown/kit/prose/view';
import type { Match } from './shared.js';

// A block around another: its type and position, a list item with its kind.
export interface ChainEntry {
    type: string;
    pos: number;
    kind?: 'bullet' | 'ordered' | 'task' | 'done';
}

// The marks of a run of text by name, a link by its address.
export type Marks = Record<string, string | true>;

export interface TextBlock {
    type: string;
    level: number;
    node: number;
    end: number;
    pos: number;
    chain: ChainEntry[];
    text: string;
    runs: [number, number, Marks][];
}

export interface TableCell {
    node: number;
    end: number;
    block: TextBlock;
}

export interface TableRow {
    node: number;
    end: number;
    cells: TableCell[];
}

export interface TableBlock {
    type: 'table';
    node: number;
    end: number;
    chain: ChainEntry[];
    rows: TableRow[];
}

// A property of the front matter: its key, its row, its value as the compare tells two apart and, where the value is a
// text, its text block.
export interface PropertyEntry {
    key: string;
    node: number;
    end: number;
    value: string;
    text: TextBlock | null;
}

// The front matter: its properties, and table where their rows end; raw is its text where it is not valid YAML.
export interface PropertiesBlock {
    type: 'properties';
    node: number;
    end: number;
    chain: ChainEntry[];
    table: number;
    entries: PropertyEntry[];
    raw: TextBlock | null;
}

export type Block = TextBlock | TableBlock | PropertiesBlock;

export interface Blocks {
    size: number;
    blocks: Block[];
}

// A difference marked over text { from, to } or over a block, row or cell { node, end }; a change of format or type
// carries its title.
export type DifferenceMark =
    | { type: 'deleted' | 'inserted' | 'format' | 'restyled'; from: number; to: number; title?: string; node?: undefined; end?: undefined }
    | { type: 'removed' | 'added' | 'changed' | 'restyled'; node: number; end: number; title?: string; from?: undefined; to?: undefined };

// What one column shows of the older document at { at } of the newer: removed text { from, to }, or a removed block,
// row or cell { node } wrapped in the blocks at wrap.
export type Deletion =
    | { at: number; from: number; to: number; node?: undefined; wrap?: undefined }
    | { at: number; node: number; wrap: number[]; from?: undefined; to?: undefined };

export interface Change {
    older: number | null;
    newer: number | null;
    at?: number;
}

export interface Differences {
    older: DifferenceMark[];
    newer: DifferenceMark[];
    deletions: Deletion[];
    pairs: [number, number][];
    changes: Change[];
    added: number;
    removed: number;
}

// The marks of a compare, each set whole by a transaction's meta: the differences, the search's matches and the spacers
// that keep the texts side by side level.
const differencesKey = new PluginKey<DecorationSet>('ec-compare-differences');
const hitsKey = new PluginKey<DecorationSet>('ec-compare-hits');
const spacersKey = new PluginKey<DecorationSet>('ec-compare-spacers');

function marksOf(key: PluginKey<DecorationSet>) {
    return new Plugin<DecorationSet>({
        key,
        state: {
            init: () => DecorationSet.empty,
            apply: (tr, value) => (tr.getMeta(key) as DecorationSet | undefined) ?? value.map(tr.mapping, tr.doc),
        },
        props: { decorations: (state) => key.getState(state) },
    });
}

function setMarks(view: EditorView, key: PluginKey<DecorationSet>, decorations: Decoration[]) {
    view.dispatch(view.state.tr.setMeta(key, DecorationSet.create(view.state.doc, decorations)));
}

// The blocks of doc in order as a compare reads them: { size, blocks }. A block is a text block or rule
// { type, level, node, end, pos, chain, text, runs }, a table { type, node, end, chain, rows } whose rows
// { node, end, cells } hold cells { node, end, block }, or the front matter { type, node, end, chain, table, entries,
// raw } whose entries are { key, node, end, value, text }. node and end bound the block, pos is where its text starts,
// runs are [from, to, marks] of its text with marks by name and a link by its address; chain holds the blocks around it,
// the outermost first, a list item with its kind: bullet, ordered, task or done.
function blocksOf(doc: ProseNode): Blocks {
    const blocks: Block[] = [];
    const chainAt = (pos: number) => {
        const $pos = doc.resolve(pos);
        const chain: ChainEntry[] = [];
        for (let depth = 1; depth <= $pos.depth; depth++) {
            const node = $pos.node(depth);
            const entry: ChainEntry = { type: node.type.name, pos: $pos.before(depth) };
            if (entry.type === 'list_item') {
                entry.kind = node.attrs.checked == null
                    ? ($pos.node(depth - 1).type.name === 'ordered_list' ? 'ordered' : 'bullet')
                    : (node.attrs.checked ? 'done' : 'task');
            }
            chain.push(entry);
        }
        return chain;
    };
    const textBlock = (node: ProseNode, pos: number, chain: ChainEntry[]): TextBlock => {
        const runs: [number, number, Marks][] = [];
        node.forEach((child, offset) => {
            if (!child.isText) return;
            const marks: Marks = {};
            for (const mark of child.marks) marks[mark.type.name] = mark.type.name === 'link' ? mark.attrs.href : true;
            runs.push([offset, offset + child.nodeSize, marks]);
        });
        return {
            type: node.type.name, level: node.attrs.level ?? 0, node: pos, end: pos + node.nodeSize, pos: pos + 1, chain,
            text: node.textBetween(0, node.content.size, null, '\uFFFC'), runs,
        };
    };
    const properties = (node: ProseNode, pos: number): PropertiesBlock => {
        const inner = node.firstChild!;
        const block: PropertiesBlock = { type: 'properties', node: pos, end: pos + node.nodeSize, chain: [], table: pos + inner.nodeSize, entries: [], raw: null };
        if (inner.type.name === 'property_raw') {
            block.raw = textBlock(inner, pos + 1, []);
            return block;
        }
        inner.forEach((row, offset) => {
            const rowPos = pos + 2 + offset;
            const [key, value] = [row.child(0), row.child(1)];
            const valuePos = rowPos + 1 + key.nodeSize;
            block.entries.push({
                key: key.textContent, node: rowPos, end: rowPos + row.nodeSize, value: JSON.stringify(value.toJSON()),
                text: value.firstChild!.type.name === 'property_text' ? textBlock(value.firstChild!, valuePos + 1, []) : null,
            });
        });
        return block;
    };
    doc.descendants((node, pos) => {
        if (node.type.name === 'front_matter') {
            blocks.push(properties(node, pos));
            return false;
        }
        if (node.type.name === 'table') {
            const rows: TableRow[] = [];
            node.forEach((row, rowOffset) => {
                const rowPos = pos + 1 + rowOffset;
                const cells: TableCell[] = [];
                row.forEach((cell, cellOffset) => {
                    const cellPos = rowPos + 1 + cellOffset;
                    cells.push({ node: cellPos, end: cellPos + cell.nodeSize, block: textBlock(cell.firstChild!, cellPos + 1, []) });
                });
                rows.push({ node: rowPos, end: rowPos + row.nodeSize, cells });
            });
            blocks.push({ type: 'table', node: pos, end: pos + node.nodeSize, chain: chainAt(pos), rows });
            return false;
        }
        if (node.isTextblock || (node.isBlock && node.isLeaf)) {
            blocks.push(textBlock(node, pos, chainAt(pos)));
            return false;
        }
        return true;
    });
    return { size: doc.content.size, blocks };
}

function element<K extends keyof HTMLElementTagNameMap>(name: K, className: string, text?: string) {
    const dom = document.createElement(name);
    dom.className = className;
    if (text !== undefined) dom.textContent = text;
    return dom;
}

// The decorations of the differences of one document. A stretch or block in another format or of another type names the
// change on a tooltip, which a tap opens as it takes the focus.
function differenceMarks(marks: DifferenceMark[]) {
    const decorations: Decoration[] = [];
    for (const mark of marks) {
        if (mark.type === 'deleted' || mark.type === 'inserted') {
            decorations.push(Decoration.inline(mark.from, mark.to, { nodeName: mark.type === 'deleted' ? 'del' : 'ins', class: 'ec-markdown-compare-' + mark.type }));
        } else if (mark.type === 'format' || mark.type === 'restyled') {
            const attrs = { class: 'ec-markdown-compare-format', 'data-tooltip': mark.title, 'data-tooltip-tap': '', tabindex: '-1' };
            decorations.push(mark.from !== undefined ? Decoration.inline(mark.from, mark.to, attrs) : Decoration.node(mark.node, mark.end, attrs));
        } else {
            decorations.push(Decoration.node(mark.node!, mark.end!, { class: 'ec-markdown-compare-' + mark.type }));
        }
    }
    return decorations;
}

// What one column shows of the older document in the newer: removed text struck through where it stood, and removed
// blocks, rows and cells before what follows them, in the blocks around them that the newer lacks.
function deletionMarks(older: ProseNode, deletions: Deletion[]) {
    const serializer = DOMSerializer.fromSchema(older.type.schema);
    return deletions.map((deletion, index) => Decoration.widget(deletion.at, () => {
        if (deletion.from !== undefined) {
            const $from = older.resolve(deletion.from);
            const dom = element('del', 'ec-markdown-compare-deleted');
            dom.append(serializer.serializeFragment($from.parent.content.cut(deletion.from - $from.start(), deletion.to - $from.start())));
            return dom;
        }
        const node = deletion.wrap.reduceRight((inner, pos) => older.nodeAt(pos)!.copy(Fragment.from(inner)), older.nodeAt(deletion.node)!);
        const dom = serializer.serializeNode(node) as HTMLElement;
        dom.classList.add('ec-markdown-compare-removed');
        return dom;
    }, { side: index - deletions.length, ignoreSelection: true }));
}

// Opens a read-only compare of the Markdown texts older and newer in parent: side by side with the blocks that face each
// other level, or with unified the newer alone with what the older had in its place. differences(older, newer) answers
// the differences of the blocks of two documents (blocksOf); match(text, query) the search's matches in the text of one
// block. labels names the older, the newer and the one-column text; scroller is the element the compare scrolls in;
// onFind runs on Ctrl+F or Cmd+F.
export async function createMarkdownCompare(parent: HTMLElement, scroller: HTMLElement, older: string, newer: string, unified: boolean, labels: string[],
    onFind: () => void, match: (text: string, query: string) => Match[], differences: (older: Blocks, newer: Blocks) => Differences) {
    const holder = parent.appendChild(element('div', unified ? 'ec-markdown-compare-column' : 'ec-markdown-compare-columns'));
    const editors: Editor[] = [];
    let level = () => {};
    let views: EditorView[] = [];
    // Side by side the front matter opens and closes on both sides at once.
    const showBoth = (open: boolean) => {
        for (const view of views) showProperties(view, open);
        level();
    };
    try {
        for (const [text, label, side] of unified ? [[newer, labels[2], 'newer']] : [[older, labels[0], 'older'], [newer, labels[1], 'newer']]) {
            const article = holder.appendChild(element('article', 'ec-preview-markdown ec-markdown-compare-text ec-markdown-compare-' + side));
            article.setAttribute('role', 'document');
            article.setAttribute('aria-label', label);
            article.tabIndex = 0;
            editors.push(await markdownEditor(article, text, {
                editable: () => false,
                attributes: () => ({ class: 'ec-markdown-content' }),
                plugins: [marksOf(differencesKey), marksOf(hitsKey), marksOf(spacersKey)],
                onProperties: showBoth,
            }));
        }
    } catch (error) {
        for (const editor of editors) editor.destroy();
        holder.remove();
        throw error;
    }

    views = editors.map((editor) => editor.action((ctx) => ctx.get(editorViewCtx)));
    // A first transaction lets the plugins settle each document before its positions are read.
    for (const view of views) {
        view.dom.removeAttribute('role');
        view.dispatch(view.state.tr);
    }
    const newerView = views.at(-1)!;
    const olderDoc = unified ? editors[0].action((ctx) => ctx.get(parserCtx))(older) : views[0].state.doc;
    const result = differences(blocksOf(olderDoc), blocksOf(newerView.state.doc));

    if (unified) {
        setMarks(newerView, differencesKey, [...differenceMarks(result.newer), ...deletionMarks(olderDoc, result.deletions)]);
    } else {
        setMarks(views[0], differencesKey, differenceMarks(result.older));
        setMarks(views[1], differencesKey, differenceMarks(result.newer));
    }
    // The front matter opens where the properties differ.
    const ends = [propertiesEnd(views[0]), propertiesEnd(newerView)];
    const inside = (marks: DifferenceMark[], end: number) => marks.some((mark) => (mark.node ?? mark.from!) < end);
    if (inside(unified ? [] : result.older, ends[0]) || inside(result.newer, ends[1]) || (unified && result.deletions.some((deletion) => deletion.at < ends[1]))) {
        showBoth(true);
    }

    // Side by side a spacer stands at the start of each block that faces one on the other side; its height brings the two
    // level. A text block holds it before its text, any other block stands after it.
    const spacers = unified ? [] : result.pairs.map(() => [element('span', 'ec-markdown-compare-spacer'), element('span', 'ec-markdown-compare-spacer')]);
    views.forEach((view, side) => {
        if (unified) return;
        const { doc } = view.state;
        setMarks(view, spacersKey, result.pairs.map((pair, index) => {
            const at = pair[side];
            return Decoration.widget(doc.nodeAt(at)!.isTextblock ? at + 1 : at, spacers[index][side], { side: -1, ignoreSelection: true });
        }));
    });
    // Two rounds: a spacer that grows from nothing can change how the margins around it collapse.
    level = () => {
        if (unified) return;
        const heights = spacers.map(() => [0, 0]);
        for (const pair of spacers) for (const spacer of pair) spacer.style.height = '';
        for (let round = 0; round < 2; round++) {
            const bottoms = spacers.map((pair) => pair.map((spacer) => spacer.getBoundingClientRect().bottom));
            const shift = [0, 0];
            const grown: [number, number][] = [];
            bottoms.forEach(([left, right], index) => {
                const gap = left + shift[0] - (right + shift[1]);
                if (Math.abs(gap) < 0.5) return;
                const side = gap > 0 ? 1 : 0;
                heights[index][side] += Math.abs(gap);
                shift[side] += Math.abs(gap);
                grown.push([index, side]);
            });
            for (const [index, side] of grown) spacers[index][side].style.height = heights[index][side] + 'px';
        }
    };
    let width = 0;
    const observer = new ResizeObserver(() => {
        if (unified || holder.clientWidth === width) return;
        width = holder.clientWidth;
        level();
    });
    observer.observe(holder);
    document.fonts?.ready.then(() => {
        if (!unified && holder.isConnected) level();
    });

    holder.addEventListener('keydown', (event) => {
        if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === 'f') {
            event.preventDefault();
            onFind();
        }
    });

    let hits: Match[][] = views.map(() => []);
    const paint = (shown: { index: number; from: number } | null) => views.forEach((view, index) => setMarks(view, hitsKey, hits[index].map(([from, to]) => Decoration.inline(from, to, {
        class: shown && shown.index === index && shown.from === from ? 'ec-markdown-hit-current' : 'ec-markdown-hit',
    }))));
    const scrollTo = (view: EditorView, pos: number) => {
        const box = scroller.getBoundingClientRect();
        scroller.scrollTop += view.coordsAtPos(pos, 1).top - box.top - box.height / 2;
    };

    return {
        summary: { added: result.added, removed: result.removed, changes: result.changes.length },
        // The matches in each document shown, the older and the newer side by side, the newer alone in one column.
        matches: (query: string) => views.map((view) => matchesIn(view.state.doc, query, match)),
        // How far down the compare a position of a document stands.
        top: (index: number, pos: number) => views[index].coordsAtPos(pos, 1).top,
        setMatches(ranges: Match[][]) {
            hits = ranges;
            paint(null);
        },
        showMatch(index: number, [from, to]: Match) {
            paint({ index, from });
            if (from < propertiesEnd(views[index])) showBoth(true);
            reveal(views[index], scroller, from, to);
        },
        changes: () => result.changes.length,
        // A change comes to the middle of the compare where the newer shows it, else where the older does.
        showChange(number: number) {
            const change = result.changes[number];
            if (unified) scrollTo(newerView, change.newer ?? change.at!);
            else if (change.newer !== null) scrollTo(views[1], change.newer);
            else scrollTo(views[0], change.older!);
        },
        destroy() {
            observer.disconnect();
            for (const editor of editors) editor.destroy();
            holder.remove();
        },
    };
}
