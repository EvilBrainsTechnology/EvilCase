// The compare of two Markdown texts, in one column or side by side.
import { editorViewCtx, parserCtx } from '@milkdown/kit/core';
import { Plugin, PluginKey } from '@milkdown/kit/prose/state';
import { Decoration, DecorationSet } from '@milkdown/kit/prose/view';
import { DOMSerializer, Fragment } from '@milkdown/kit/prose/model';
import { markdownEditor, matchesIn, reveal } from './shared.js';

// The marks of a compare, each set whole by a transaction's meta: the differences, the search's matches and the spacers
// that keep the texts side by side level.
const differencesKey = new PluginKey('ec-compare-differences');
const hitsKey = new PluginKey('ec-compare-hits');
const spacersKey = new PluginKey('ec-compare-spacers');

function marksOf(key) {
    return new Plugin({
        key,
        state: {
            init: () => DecorationSet.empty,
            apply: (tr, value) => tr.getMeta(key) ?? value.map(tr.mapping, tr.doc),
        },
        props: { decorations: (state) => key.getState(state) },
    });
}

function setMarks(view, key, decorations) {
    view.dispatch(view.state.tr.setMeta(key, DecorationSet.create(view.state.doc, decorations)));
}

// The blocks of doc in order as a compare reads them: { size, blocks }. A block is a text block or rule
// { type, level, node, end, pos, chain, text, runs }, or a table { type, node, end, chain, rows } whose rows
// { node, end, cells } hold cells { node, end, block }. node and end bound the block, pos is where its text starts,
// runs are [from, to, marks] of its text with marks by name and a link by its address; chain holds the blocks around it,
// the outermost first, a list item with its kind: bullet, ordered, task or done.
function blocksOf(doc) {
    const blocks = [];
    const chainAt = (pos) => {
        const $pos = doc.resolve(pos);
        const chain = [];
        for (let depth = 1; depth <= $pos.depth; depth++) {
            const node = $pos.node(depth);
            const entry = { type: node.type.name, pos: $pos.before(depth) };
            if (entry.type === 'list_item') {
                entry.kind = node.attrs.checked == null
                    ? ($pos.node(depth - 1).type.name === 'ordered_list' ? 'ordered' : 'bullet')
                    : (node.attrs.checked ? 'done' : 'task');
            }
            chain.push(entry);
        }
        return chain;
    };
    const textBlock = (node, pos, chain) => {
        const runs = [];
        node.forEach((child, offset) => {
            if (!child.isText) return;
            const marks = {};
            for (const mark of child.marks) marks[mark.type.name] = mark.type.name === 'link' ? mark.attrs.href : true;
            runs.push([offset, offset + child.nodeSize, marks]);
        });
        return {
            type: node.type.name, level: node.attrs.level ?? 0, node: pos, end: pos + node.nodeSize, pos: pos + 1, chain,
            text: node.textBetween(0, node.content.size, null, '\uFFFC'), runs,
        };
    };
    doc.descendants((node, pos) => {
        if (node.type.name === 'table') {
            const rows = [];
            node.forEach((row, rowOffset) => {
                const rowPos = pos + 1 + rowOffset;
                const cells = [];
                row.forEach((cell, cellOffset) => {
                    const cellPos = rowPos + 1 + cellOffset;
                    cells.push({ node: cellPos, end: cellPos + cell.nodeSize, block: textBlock(cell.firstChild, cellPos + 1, []) });
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

function element(name, className, text) {
    const dom = document.createElement(name);
    dom.className = className;
    if (text !== undefined) dom.textContent = text;
    return dom;
}

// The decorations of the differences of one document. A stretch or block in another format or of another type names the
// change on a tooltip, which a tap opens as it takes the focus.
function differenceMarks(marks) {
    const decorations = [];
    for (const mark of marks) {
        if (mark.type === 'deleted' || mark.type === 'inserted') {
            decorations.push(Decoration.inline(mark.from, mark.to, { nodeName: mark.type === 'deleted' ? 'del' : 'ins', class: 'ec-markdown-compare-' + mark.type }));
        } else if (mark.type === 'format' || mark.type === 'restyled') {
            const attrs = { class: 'ec-markdown-compare-format', 'data-tooltip': mark.title, 'data-tooltip-tap': '', tabindex: '-1' };
            decorations.push(mark.from !== undefined ? Decoration.inline(mark.from, mark.to, attrs) : Decoration.node(mark.node, mark.end, attrs));
        } else {
            decorations.push(Decoration.node(mark.node, mark.end, { class: 'ec-markdown-compare-' + mark.type }));
        }
    }
    return decorations;
}

// What one column shows of the older document in the newer: removed text struck through where it stood, and removed
// blocks, rows and cells before what follows them, in the blocks around them that the newer lacks.
function deletionMarks(older, deletions) {
    const serializer = DOMSerializer.fromSchema(older.type.schema);
    return deletions.map((deletion, index) => Decoration.widget(deletion.at, () => {
        if (deletion.from !== undefined) {
            const $from = older.resolve(deletion.from);
            const dom = element('del', 'ec-markdown-compare-deleted');
            dom.append(serializer.serializeFragment($from.parent.content.cut(deletion.from - $from.start(), deletion.to - $from.start())));
            return dom;
        }
        const node = deletion.wrap.reduceRight((inner, pos) => older.nodeAt(pos).copy(Fragment.from(inner)), older.nodeAt(deletion.node));
        const dom = serializer.serializeNode(node);
        dom.classList.add('ec-markdown-compare-removed');
        return dom;
    }, { side: index - deletions.length, ignoreSelection: true }));
}

// Opens a read-only compare of the Markdown texts older and newer in parent: side by side with the blocks that face each
// other level, or with unified the newer alone with what the older had in its place. differences(older, newer) answers
// the differences of the blocks of two documents (blocksOf); match(text, query) the search's matches in the text of one
// block. labels names the older, the newer and the one-column text; scroller is the element the compare scrolls in;
// onFind runs on Ctrl+F or Cmd+F.
export async function createMarkdownCompare(parent, scroller, older, newer, unified, labels, onFind, match, differences) {
    const holder = parent.appendChild(element('div', unified ? 'ec-markdown-compare-column' : 'ec-markdown-compare-columns'));
    const editors = [];
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
            }));
        }
    } catch (error) {
        for (const editor of editors) editor.destroy();
        holder.remove();
        throw error;
    }

    const views = editors.map((editor) => editor.action((ctx) => ctx.get(editorViewCtx)));
    // A first transaction lets the plugins settle each document before its positions are read.
    for (const view of views) {
        view.dom.removeAttribute('role');
        view.dispatch(view.state.tr);
    }
    const newerView = views.at(-1);
    const olderDoc = unified ? editors[0].action((ctx) => ctx.get(parserCtx))(older) : views[0].state.doc;
    const result = differences(blocksOf(olderDoc), blocksOf(newerView.state.doc));

    if (unified) {
        setMarks(newerView, differencesKey, [...differenceMarks(result.newer), ...deletionMarks(olderDoc, result.deletions)]);
    } else {
        setMarks(views[0], differencesKey, differenceMarks(result.older));
        setMarks(views[1], differencesKey, differenceMarks(result.newer));
    }

    // Side by side a spacer stands at the start of each block that faces one on the other side; its height brings the two
    // level. A text block holds it before its text, any other block stands after it.
    const spacers = unified ? [] : result.pairs.map(() => [element('span', 'ec-markdown-compare-spacer'), element('span', 'ec-markdown-compare-spacer')]);
    views.forEach((view, side) => {
        if (unified) return;
        const { doc } = view.state;
        setMarks(view, spacersKey, result.pairs.map((pair, index) => {
            const at = pair[side];
            return Decoration.widget(doc.nodeAt(at).isTextblock ? at + 1 : at, spacers[index][side], { side: -1, ignoreSelection: true });
        }));
    });
    // Two rounds: a spacer that grows from nothing can change how the margins around it collapse.
    const level = () => {
        const heights = spacers.map(() => [0, 0]);
        for (const pair of spacers) for (const spacer of pair) spacer.style.height = '';
        for (let round = 0; round < 2; round++) {
            const bottoms = spacers.map((pair) => pair.map((spacer) => spacer.getBoundingClientRect().bottom));
            const shift = [0, 0];
            const grown = [];
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

    let hits = views.map(() => []);
    const paint = (shown) => views.forEach((view, index) => setMarks(view, hitsKey, hits[index].map(([from, to]) => Decoration.inline(from, to, {
        class: shown && shown.index === index && shown.from === from ? 'ec-markdown-hit-current' : 'ec-markdown-hit',
    }))));
    const scrollTo = (view, pos) => {
        const box = scroller.getBoundingClientRect();
        scroller.scrollTop += view.coordsAtPos(pos, 1).top - box.top - box.height / 2;
    };

    return {
        summary: { added: result.added, removed: result.removed, changes: result.changes.length },
        // The matches in each document shown, the older and the newer side by side, the newer alone in one column.
        matches: (query) => views.map((view) => matchesIn(view.state.doc, query, match)),
        // How far down the compare a position of a document stands.
        top: (index, pos) => views[index].coordsAtPos(pos, 1).top,
        setMatches(ranges) {
            hits = ranges;
            paint(null);
        },
        showMatch(index, [from, to]) {
            paint({ index, from });
            reveal(views[index], scroller, from, to);
        },
        changes: () => result.changes.length,
        // A change comes to the middle of the compare where the newer shows it, else where the older does.
        showChange(number) {
            const change = result.changes[number];
            if (unified) scrollTo(newerView, change.newer ?? change.at);
            else if (change.newer !== null) scrollTo(views[1], change.newer);
            else scrollTo(views[0], change.older);
        },
        destroy() {
            observer.disconnect();
            for (const editor of editors) editor.destroy();
            holder.remove();
        },
    };
}
