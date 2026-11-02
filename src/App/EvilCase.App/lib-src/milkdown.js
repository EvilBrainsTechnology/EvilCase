// The Markdown view, editor and compare: Milkdown with CommonMark, GFM, history, the clipboard and the marks of the app's
// search, never Crepe. A link stays a link only to a web or e-mail address, images are never loaded, raw HTML never runs
// and a heading carries no id.

import { Editor, commandsCtx, defaultValueCtx, editorViewCtx, editorViewOptionsCtx, parserCtx, rootCtx, serializerCtx } from '@milkdown/kit/core';
import { commonmark, headingSchema, htmlSchema, imageSchema, linkSchema, remarkPreserveEmptyLinePlugin } from '@milkdown/kit/preset/commonmark';
import { gfm, insertTableCommand } from '@milkdown/kit/preset/gfm';
import { history } from '@milkdown/kit/plugin/history';
import { clipboard } from '@milkdown/kit/plugin/clipboard';
import { $prose, replaceAll } from '@milkdown/kit/utils';
import { Plugin, PluginKey, TextSelection } from '@milkdown/kit/prose/state';
import { Decoration, DecorationSet } from '@milkdown/kit/prose/view';
import { DOMSerializer, Fragment } from '@milkdown/kit/prose/model';
import { keymap } from '@milkdown/kit/prose/keymap';
import { redo, undo, undoDepth } from '@milkdown/kit/prose/history';
import { lift, setBlockType, toggleMark, wrapIn } from '@milkdown/kit/prose/commands';
import { liftListItem, wrapInList } from '@milkdown/kit/prose/schema-list';

// A link stays a link only to a web or e-mail address; any other shows as its text.
const safeLink = /^(https?:|mailto:)/i;

// Pasted HTML keeps only safe links; a Markdown link to anything else keeps its address in the file and shows as text.
const links = linkSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [{
        tag: 'a[href]',
        getAttrs: (dom) => (safeLink.test(dom.getAttribute('href') || '') ? { href: dom.getAttribute('href'), title: dom.getAttribute('title') } : false),
    }],
    toDOM: (mark) => (safeLink.test(mark.attrs.href)
        ? ['a', { href: mark.attrs.href, title: mark.attrs.title || null, target: '_blank', rel: 'noopener noreferrer' }, 0]
        : ['span', 0]),
}));

// An image is never loaded: it shows its description, or the word obrázek, and keeps its address in the file. A pasted
// img is dropped.
const images = imageSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [{
        tag: 'span[data-type="image"]',
        getAttrs: (dom) => ({ src: dom.getAttribute('data-src') || '', alt: dom.getAttribute('data-alt') || '', title: dom.getAttribute('data-title') || '' }),
    }],
    toDOM: (node) => ['span', {
        class: 'ec-markdown-image', 'data-type': 'image', 'data-src': node.attrs.src, 'data-alt': node.attrs.alt,
        'data-title': node.attrs.title, title: node.attrs.title || null,
    }, node.attrs.alt || 'obrázek'],
}));

// Raw HTML never runs: it shows as its text and stays in the file as written; pasted HTML never becomes raw HTML.
const html = htmlSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    parseDOM: [],
    toDOM: (node) => ['span', { class: 'ec-markdown-html' }, node.attrs.value],
}));

// A heading carries no id, so the text of a file never names an element of the page.
const headings = headingSchema.extendSchema((prev) => (ctx) => ({
    ...prev(ctx),
    toDOM: (node) => ['h' + node.attrs.level, 0],
}));

function within(state, name) {
    const { $from } = state.selection;
    for (let depth = $from.depth; depth >= 0; depth--) {
        if ($from.node(depth).type.name === name) return true;
    }
    return false;
}

function headingLevel(state) {
    const { parent } = state.selection.$from;
    return parent.type.name === 'heading' ? parent.attrs.level : 0;
}

function markOn(name) {
    return (state) => {
        const type = state.schema.marks[name];
        const { from, to, empty, $from } = state.selection;
        return empty ? Boolean(type.isInSet(state.storedMarks || $from.marks())) : state.doc.rangeHasMark(from, to, type);
    };
}

// The nearest list around the selection and its kind: bullet, ordered or task.
function listAt(state) {
    const { $from } = state.selection;
    for (let depth = $from.depth; depth > 0; depth--) {
        const node = $from.node(depth);
        if (node.type.name === 'ordered_list') return { node, pos: $from.before(depth), kind: 'ordered' };
        if (node.type.name === 'bullet_list') {
            return { node, pos: $from.before(depth), kind: node.firstChild && node.firstChild.attrs.checked != null ? 'task' : 'bullet' };
        }
    }
    return null;
}

// Turns the nearest list around the selection of tr into a list of kind, its items with it.
function retype(tr, kind) {
    const found = listAt(tr);
    if (!found) return;
    const { nodes } = tr.doc.type.schema;
    tr.setNodeMarkup(found.pos, kind === 'ordered' ? nodes.ordered_list : nodes.bullet_list, { spread: found.node.attrs.spread });
    let pos = found.pos + 1;
    found.node.forEach((item) => {
        tr.setNodeMarkup(pos, undefined, {
            ...item.attrs,
            listType: kind === 'ordered' ? 'ordered' : 'bullet',
            checked: kind === 'task' ? item.attrs.checked === true : null,
        });
        pos += item.nodeSize;
    });
}

// The link around the cursor: its range and its address.
function linkAt(state) {
    const type = state.schema.marks.link;
    const { $from } = state.selection;
    const parent = $from.parent;
    let index = $from.index();
    let found = index < parent.childCount ? type.isInSet(parent.child(index).marks) : null;
    if (!found && $from.textOffset === 0 && index > 0) {
        index -= 1;
        found = type.isInSet(parent.child(index).marks);
    }
    if (!found) return null;
    let first = index;
    let last = index;
    while (first > 0 && found.isInSet(parent.child(first - 1).marks)) first -= 1;
    while (last + 1 < parent.childCount && found.isInSet(parent.child(last + 1).marks)) last += 1;
    let from = $from.start();
    for (let i = 0; i < first; i++) from += parent.child(i).nodeSize;
    let to = from;
    for (let i = first; i <= last; i++) to += parent.child(i).nodeSize;
    return { from, to, href: found.attrs.href };
}

function heading(level) {
    return (state, dispatch) => (headingLevel(state) === level
        ? setBlockType(state.schema.nodes.paragraph)(state, dispatch)
        : setBlockType(state.schema.nodes.heading, { level })(state, dispatch));
}

function mark(name) {
    return (state, dispatch) => toggleMark(state.schema.marks[name])(state, dispatch);
}

// A second press of the same list undoes it; another list turns into the pressed one.
function list(kind) {
    return (state, dispatch) => {
        const found = listAt(state);
        if (found && found.kind === kind) return liftListItem(state.schema.nodes.list_item)(state, dispatch);
        if (found) {
            if (dispatch) {
                const tr = state.tr;
                retype(tr, kind);
                dispatch(tr.scrollIntoView());
            }
            return true;
        }
        const type = kind === 'ordered' ? state.schema.nodes.ordered_list : state.schema.nodes.bullet_list;
        return wrapInList(type)(state, dispatch && ((tr) => {
            retype(tr, kind);
            dispatch(tr);
        }));
    };
}

function quote(state, dispatch) {
    return within(state, 'blockquote') ? lift(state, dispatch) : wrapIn(state.schema.nodes.blockquote)(state, dispatch);
}

// A selection within one paragraph becomes inline code, any other place a code block; a code block goes back to a paragraph.
function code(state, dispatch) {
    const { $from, $to, empty } = state.selection;
    if ($from.parent.type.name === 'code_block') return setBlockType(state.schema.nodes.paragraph)(state, dispatch);
    if (!empty && $from.sameParent($to)) return toggleMark(state.schema.marks.inlineCode)(state, dispatch);
    return setBlockType(state.schema.nodes.code_block)(state, dispatch);
}

// Which toolbar commands are on at the cursor.
const checks = {
    heading1: (state) => headingLevel(state) === 1,
    heading2: (state) => headingLevel(state) === 2,
    heading3: (state) => headingLevel(state) === 3,
    strong: markOn('strong'),
    emphasis: markOn('emphasis'),
    strike: markOn('strike_through'),
    bulletList: (state) => listAt(state)?.kind === 'bullet',
    orderedList: (state) => listAt(state)?.kind === 'ordered',
    taskList: (state) => listAt(state)?.kind === 'task',
    quote: (state) => within(state, 'blockquote'),
    code: (state) => within(state, 'code_block') || markOn('inlineCode')(state),
    link: (state) => linkAt(state) !== null,
};

// A task's checkbox is in the tab order and announced as changeable only while the text is editable.
function markBox(box, editable) {
    box.tabIndex = editable ? 0 : -1;
    if (editable) box.removeAttribute('aria-readonly');
    else box.setAttribute('aria-readonly', 'true');
}

// A task item: a real checkbox, which only an edit can change. editable() answers whether the text is editable now.
function taskItemView(editable) {
    return (node, view, getPos) => {
        if (node.attrs.checked == null) return null;
        let current = node;
        const dom = document.createElement('li');
        const holder = document.createElement('span');
        const box = document.createElement('input');
        const contentDOM = document.createElement('div');
        dom.className = 'ec-markdown-task';
        holder.className = 'ec-markdown-task-mark';
        holder.contentEditable = 'false';
        box.type = 'checkbox';
        box.className = 'ec-markdown-task-box';
        box.setAttribute('aria-label', 'Splněno');
        contentDOM.className = 'ec-markdown-task-text';
        holder.append(box);
        dom.append(holder, contentDOM);
        const paint = () => {
            box.checked = current.attrs.checked === true;
            dom.setAttribute('data-checked', String(current.attrs.checked === true));
            markBox(box, editable());
        };
        box.addEventListener('mousedown', (event) => event.preventDefault());
        box.addEventListener('click', (event) => {
            if (!editable()) event.preventDefault();
        });
        box.addEventListener('change', () => {
            const pos = getPos();
            if (!editable() || typeof pos !== 'number') return;
            view.dispatch(view.state.tr.setNodeMarkup(pos, undefined, { ...current.attrs, checked: box.checked }));
        });
        paint();
        return {
            dom,
            contentDOM,
            update: (next) => {
                if (next.type !== current.type || next.attrs.checked == null) return false;
                current = next;
                paint();
                return true;
            },
            stopEvent: (event) => holder.contains(event.target),
            ignoreMutation: (mutation) => mutation.type !== 'selection'
                && (holder.contains(mutation.target) || (mutation.type === 'attributes' && mutation.target === dom)),
        };
    };
}

// Makes a Milkdown editor of text in parent with the schema every Markdown view shares. editable() and attributes()
// answer the view's state now; plugins are ProseMirror plugins of its own.
function markdownEditor(parent, text, { editable, attributes, plugins, history: withHistory }) {
    let editor = Editor.make()
        .config((ctx) => {
            ctx.set(rootCtx, parent);
            ctx.set(defaultValueCtx, text);
            ctx.update(editorViewOptionsCtx, (options) => ({
                ...options,
                editable,
                attributes,
                nodeViews: { list_item: taskItemView(editable) },
                // A dropped file is not inserted.
                handleDrop: (view, event) => Boolean(event.dataTransfer && event.dataTransfer.files.length > 0),
            }));
        })
        // An empty paragraph is not written as raw HTML.
        .use(commonmark.filter((plugin) => !remarkPreserveEmptyLinePlugin.includes(plugin)))
        .use(gfm);
    if (withHistory) editor = editor.use(history);
    editor = editor
        .use(clipboard)
        .use(links)
        .use(images)
        .use(html)
        .use(headings);
    for (const plugin of plugins) editor = editor.use($prose(() => plugin));
    return editor.create();
}

// The matches of query in doc as [from, to] positions. A match stays within one block; an image, raw HTML or a line
// break inside a block is a character no query matches. match(text, query) answers the matches in the text of one block.
function matchesIn(doc, query, match) {
    const found = [];
    if (!query) return found;
    doc.descendants((node, pos) => {
        if (!node.isTextblock) return true;
        for (const [from, to] of match(node.textBetween(0, node.content.size, null, '\uFFFC'), query)) {
            found.push([pos + 1 + from, pos + 1 + to]);
        }
        return false;
    });
    return found;
}

// The range comes into the middle of scroller, after a code block or table that scrolls sideways brings it in.
function reveal(view, scroller, from, to) {
    const { node } = view.domAtPos(from);
    const block = (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement)?.closest('pre, .tableWrapper');
    if (block && block.scrollWidth > block.clientWidth) {
        const inner = view.coordsAtPos(from, 1);
        const outer = block.getBoundingClientRect();
        if (inner.left < outer.left || inner.right > outer.right) block.scrollLeft += inner.left - outer.left - outer.width / 2;
    }
    const start = view.coordsAtPos(from, 1);
    const end = view.coordsAtPos(to, -1);
    const box = scroller.getBoundingClientRect();
    if (start.top < box.top || end.bottom > box.bottom) {
        scroller.scrollTop += start.top - box.top - (box.height - (end.bottom - start.top)) / 2;
    }
}

// The search of the edited text: the query and its matches as [from, to] positions, mapped through an edit until they
// are counted again.
const searchKey = new PluginKey('ec-search');
const recountDelay = 250;

// Opens the Markdown text in parent, read-only unless editable. While editable, onChange(dirty, active, disabled) hears
// whether the text changed since the edit began and which toolbar commands are on and unavailable at the cursor; onFind
// runs on Ctrl+F or Cmd+F; onMatches(current, total) hears the search's position and count while a query is set.
// match(text, query) answers the matches of query in the text of one block as [from, to] offsets.
export async function createMarkdown(parent, text, editable, label, onChange, onFind, onMatches, match) {
    const state = { editable, view: null, initial: null, reported: '', commands: null };

    const commands = {
        undo,
        redo,
        heading1: heading(1),
        heading2: heading(2),
        heading3: heading(3),
        strong: mark('strong'),
        emphasis: mark('emphasis'),
        strike: mark('strike_through'),
        bulletList: list('bullet'),
        orderedList: list('ordered'),
        taskList: list('task'),
        quote,
        code,
        // The address is asked by the app; here only whether a link can stand at the cursor.
        link: (s) => !within(s, 'code_block'),
        table: (s, dispatch) => {
            if (within(s, 'table') || within(s, 'code_block')) return false;
            if (!dispatch) return true;
            return state.commands.get(insertTableCommand.key)({ row: 3, col: 3 })(s, dispatch);
        },
    };

    const report = (force) => {
        const { view } = state;
        if (!view || !state.editable) return;
        const s = view.state;
        const dirty = undoDepth(s) > 0 && !s.doc.eq(state.initial);
        const active = Object.keys(checks).filter((name) => checks[name](s));
        const disabled = Object.keys(commands).filter((name) => !commands[name](s));
        const next = JSON.stringify([dirty, active, disabled]);
        if (!force && next === state.reported) return;
        state.reported = next;
        onChange(dirty, active, disabled);
    };

    // The stage the text scrolls in.
    const scroller = parent.parentElement ?? parent;
    let counted = [0, 0];
    let recount = 0;
    // While find runs, its own steps report nothing.
    let finding = false;

    // The number of matches starting at or before at.
    const startingBy = (matches, at) => {
        let low = 0;
        let high = matches.length;
        while (low < high) {
            const middle = (low + high) >> 1;
            if (matches[middle][0] <= at) low = middle + 1; else high = middle;
        }
        return low;
    };

    const marked = (doc, matches) => DecorationSet.create(doc, matches.map(([from, to]) => Decoration.inline(from, to, { class: 'ec-markdown-hit' })));

    // The current match is the last one starting at or before the selection. always reports even an unchanged count:
    // the field waits for the count of a new query.
    const reportMatches = (view, always) => {
        const { query, matches } = searchKey.getState(view.state);
        if (!query) {
            counted = [0, 0];
            return;
        }
        const total = matches.length;
        const current = total > 0 ? Math.max(startingBy(matches, view.state.selection.from), 1) : 0;
        if (always || current !== counted[0] || total !== counted[1]) {
            counted = [current, total];
            onMatches(current, total);
        }
    };

    const search = new Plugin({
        key: searchKey,
        state: {
            init: () => ({ query: '', matches: [], marks: DecorationSet.empty, stale: false }),
            apply(tr, value, previous, next) {
                const query = tr.getMeta(searchKey);
                if (query !== undefined) {
                    const matches = matchesIn(next.doc, query, match);
                    return { query, matches, marks: marked(next.doc, matches), stale: false };
                }
                if (!tr.docChanged || !value.query) return value;
                const matches = [];
                for (const [from, to] of value.matches) {
                    const start = tr.mapping.map(from, 1);
                    const end = tr.mapping.map(to, -1);
                    if (start < end) matches.push([start, end]);
                }
                return { query: value.query, matches, marks: value.marks.map(tr.mapping, tr.doc), stale: true };
            },
        },
        props: {
            // The match the selection covers is drawn apart from the rest.
            decorations(editorState) {
                const { matches, marks } = searchKey.getState(editorState);
                const { from, to } = editorState.selection;
                const shown = matches[startingBy(matches, from) - 1];
                if (!shown || shown[0] !== from || shown[1] !== to) return marks;
                return marks.add(editorState.doc, [Decoration.inline(from, to, { class: 'ec-markdown-hit-current' })]);
            },
        },
        // An edit counts the query again once the typing pauses and no composition is under way.
        view: () => {
            const recountLater = (view) => {
                clearTimeout(recount);
                recount = setTimeout(() => {
                    if (view.composing) recountLater(view);
                    else view.dispatch(view.state.tr.setMeta(searchKey, searchKey.getState(view.state).query));
                }, recountDelay);
            };
            return {
                update(view) {
                    if (searchKey.getState(view.state).stale) recountLater(view);
                    if (!finding) reportMatches(view, false);
                },
                destroy: () => clearTimeout(recount),
            };
        },
    });

    const editor = await markdownEditor(parent, text, {
        editable: () => state.editable,
        attributes: () => (state.editable
            ? { class: 'ec-markdown-content', role: 'textbox', 'aria-multiline': 'true', 'aria-label': label }
            : { class: 'ec-markdown-content' }),
        history: true,
        plugins: [
            new Plugin({ view: () => ({ update: () => report(false) }) }),
            search,
            keymap({ 'Mod-f': () => { onFind(); return true; } }),
        ],
    });

    const [view, serialize, manager] = editor.action((ctx) => [ctx.get(editorViewCtx), ctx.get(serializerCtx), ctx.get(commandsCtx)]);
    const markBoxes = () => {
        for (const box of view.dom.querySelectorAll('.ec-markdown-task-box')) markBox(box, state.editable);
    };
    const selectMatch = ([from, to]) => {
        view.dispatch(view.state.tr.setSelection(TextSelection.create(view.state.doc, from, to)));
        reveal(view, scroller, from, to);
    };

    state.commands = manager;
    state.view = view;
    // Plugins have settled the document by now; a change is what comes after.
    state.initial = view.state.doc;
    // The read-only view is a document, not a text field.
    if (!state.editable) view.dom.removeAttribute('role');
    report(true);

    return {
        editable: () => state.editable,
        text: () => serialize(view.state.doc),
        focus: () => view.focus(),
        // Turns the shown text editable in place; a change is what comes after.
        edit() {
            state.editable = true;
            view.setProps({});
            markBoxes();
            state.initial = view.state.doc;
            report(true);
            view.focus();
        },
        // Shows the text read-only, without history.
        show(markdown) {
            view.dispatch(view.state.tr.setMeta(searchKey, ''));
            state.editable = false;
            editor.action(replaceAll(markdown, true));
            view.setProps({});
            view.dom.removeAttribute('role');
            markBoxes();
            state.initial = view.state.doc;
        },
        run(name) {
            if (!Object.hasOwn(commands, name)) return;
            commands[name](view.state, view.dispatch);
            view.focus();
        },
        link() {
            const found = linkAt(view.state);
            return found ? found.href : '';
        },
        // Links the selection, the link at the cursor or, with nothing selected, the address itself; empty removes the link.
        setLink(href) {
            if (href && !safeLink.test(href)) return;
            const s = view.state;
            const type = s.schema.marks.link;
            const found = linkAt(s);
            const { from, to } = found && s.selection.empty ? found : s.selection;
            const tr = s.tr.removeMark(from, to, type);
            if (href && from === to) {
                tr.insertText(href, from);
                tr.addMark(from, from + href.length, type.create({ href }));
            } else if (href) {
                tr.addMark(from, to, type.create({ href }));
            }
            view.dispatch(tr.scrollIntoView());
        },
        // A new query selects its first match from the cursor on; again steps to the next or, with previous, the previous
        // match. Both wrap around.
        find(query, again, previous) {
            const { from, to } = view.state.selection;
            const shown = searchKey.getState(view.state);
            const fresh = !again || shown.query !== query;
            finding = true;
            try {
                if (fresh) {
                    view.dispatch(view.state.tr.setMeta(searchKey, query));
                    const { matches } = searchKey.getState(view.state);
                    if (matches.length > 0) selectMatch(matches.find(([start]) => start >= from) ?? matches[0]);
                } else if (shown.matches.length > 0) {
                    const { matches } = shown;
                    selectMatch(previous
                        ? matches.findLast(([, end]) => end <= from) ?? matches.at(-1)
                        : matches.find(([start]) => start >= to) ?? matches[0]);
                }
            } finally {
                finding = false;
            }
            reportMatches(view, fresh);
        },
        destroy: () => editor.destroy(),
    };
}

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

// The decorations of the differences of one document. A block names its changes of type and format on a label after its
// text; a stretch in another format also on a tooltip.
function differenceMarks(doc, marks) {
    const decorations = [];
    for (const mark of marks) {
        if (mark.type === 'deleted' || mark.type === 'inserted') {
            decorations.push(Decoration.inline(mark.from, mark.to, { nodeName: mark.type === 'deleted' ? 'del' : 'ins', class: 'ec-markdown-compare-' + mark.type }));
        } else if (mark.type === 'format') {
            decorations.push(Decoration.inline(mark.from, mark.to, { class: 'ec-markdown-compare-format', 'data-format': mark.title }));
        } else if (mark.type === 'restyled') {
            const node = doc.nodeAt(mark.node);
            decorations.push(node.isTextblock && node.content.size > 0
                ? Decoration.inline(mark.node + 1, mark.end - 1, { class: 'ec-markdown-compare-format' })
                : Decoration.node(mark.node, mark.end, { class: 'ec-markdown-compare-format' }));
        } else if (mark.type === 'named') {
            const at = doc.nodeAt(mark.node).isTextblock ? mark.end - 1 : mark.end;
            decorations.push(Decoration.widget(at, () => element('span', 'ec-markdown-compare-chip', mark.title), { side: 1, ignoreSelection: true }));
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
        setMarks(newerView, differencesKey, [...differenceMarks(newerView.state.doc, result.newer), ...deletionMarks(olderDoc, result.deletions)]);
    } else {
        setMarks(views[0], differencesKey, differenceMarks(views[0].state.doc, result.older));
        setMarks(views[1], differencesKey, differenceMarks(views[1].state.doc, result.newer));
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
