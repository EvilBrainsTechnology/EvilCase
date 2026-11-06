// The schema, the commands and the helpers every Markdown view shares.
import { Editor, defaultValueCtx, editorViewOptionsCtx, rootCtx } from '@milkdown/kit/core';
import { commonmark, headingSchema, htmlSchema, imageSchema, linkSchema, remarkPreserveEmptyLinePlugin } from '@milkdown/kit/preset/commonmark';
import { gfm } from '@milkdown/kit/preset/gfm';
import { history } from '@milkdown/kit/plugin/history';
import { clipboard } from '@milkdown/kit/plugin/clipboard';
import { $prose } from '@milkdown/kit/utils';
import { lift, setBlockType, toggleMark, wrapIn } from '@milkdown/kit/prose/commands';
import { liftListItem, wrapInList } from '@milkdown/kit/prose/schema-list';
import type { Node as ProseNode } from '@milkdown/kit/prose/model';
import type { Command, EditorState, Plugin, Selection, Transaction } from '@milkdown/kit/prose/state';
import type { EditorView, NodeView, NodeViewConstructor, ViewMutationRecord } from '@milkdown/kit/prose/view';
import { frontMatterPlugins, keepFrontMatter, propertiesView } from './properties.js';

// [from, to] positions or offsets.
export type Match = [number, number];

// The kind of a list: bullet, ordered or task.
export type ListKind = 'bullet' | 'ordered' | 'task';

type Dispatch = (tr: Transaction) => void;

// A link stays a link only to a web or e-mail address; any other shows as its text.
export const safeLink = /^(https?:|mailto:)/i;

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

export function within(state: EditorState, name: string) {
    const { $from } = state.selection;
    for (let depth = $from.depth; depth >= 0; depth--) {
        if ($from.node(depth).type.name === name) return true;
    }
    return false;
}

function headingLevel(state: EditorState): number {
    const { parent } = state.selection.$from;
    return parent.type.name === 'heading' ? parent.attrs.level : 0;
}

function markOn(name: string) {
    return (state: EditorState) => {
        const type = state.schema.marks[name];
        const { from, to, empty, $from } = state.selection;
        return empty ? Boolean(type.isInSet(state.storedMarks || $from.marks())) : state.doc.rangeHasMark(from, to, type);
    };
}

// The nearest list around the selection and its kind: bullet, ordered or task.
function listAt(state: { selection: Selection }): { node: ProseNode; pos: number; kind: ListKind } | null {
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
function retype(tr: Transaction, kind: ListKind) {
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
export function linkAt(state: EditorState) {
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

export function heading(level: number): Command {
    return (state, dispatch) => (headingLevel(state) === level
        ? setBlockType(state.schema.nodes.paragraph)(state, dispatch)
        : setBlockType(state.schema.nodes.heading, { level })(state, dispatch));
}

export function mark(name: string): Command {
    return (state, dispatch) => toggleMark(state.schema.marks[name])(state, dispatch);
}

// A second press of the same list undoes it; another list turns into the pressed one.
export function list(kind: ListKind): Command {
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
        return wrapInList(type)(state, dispatch && ((tr: Transaction) => {
            retype(tr, kind);
            dispatch(tr);
        }));
    };
}

export function quote(state: EditorState, dispatch?: Dispatch) {
    return within(state, 'blockquote') ? lift(state, dispatch) : wrapIn(state.schema.nodes.blockquote)(state, dispatch);
}

// A selection within one paragraph becomes inline code, any other place a code block; a code block goes back to a paragraph.
export function code(state: EditorState, dispatch?: Dispatch) {
    const { $from, $to, empty } = state.selection;
    if ($from.parent.type.name === 'code_block') return setBlockType(state.schema.nodes.paragraph)(state, dispatch);
    if (!empty && $from.sameParent($to)) return toggleMark(state.schema.marks.inlineCode)(state, dispatch);
    return setBlockType(state.schema.nodes.code_block)(state, dispatch);
}

// Which toolbar commands are on at the cursor.
export const checks: Record<string, (state: EditorState) => boolean> = {
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
export function markBox(box: HTMLInputElement, editable: boolean) {
    box.tabIndex = editable ? 0 : -1;
    if (editable) box.removeAttribute('aria-readonly');
    else box.setAttribute('aria-readonly', 'true');
}

// A task item: a real checkbox, which only an edit can change. editable() answers whether the text is editable now. Any
// other list item answers null and ProseMirror draws it itself.
function taskItemView(editable: () => boolean) {
    return (node: ProseNode, view: EditorView, getPos: () => number | undefined): NodeView | null => {
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
            update: (next: ProseNode) => {
                if (next.type !== current.type || next.attrs.checked == null) return false;
                current = next;
                paint();
                return true;
            },
            stopEvent: (event: Event) => holder.contains(event.target as Node),
            ignoreMutation: (mutation: ViewMutationRecord) => mutation.type !== 'selection'
                && (holder.contains(mutation.target) || (mutation.type === 'attributes' && mutation.target === dom)),
        };
    };
}

// Makes a Milkdown editor of text in parent with the schema every Markdown view shares. editable() and attributes()
// answer the view's state now; plugins are ProseMirror plugins of its own; onProperties(open) hears the front matter's
// button.
export function markdownEditor(parent: HTMLElement, text: string, { editable, attributes, plugins, history: withHistory, onProperties }: {
    editable: () => boolean;
    attributes: () => Record<string, string>;
    plugins: Plugin[];
    history?: boolean;
    onProperties?: (open: boolean) => void;
}) {
    let editor = Editor.make()
        .config((ctx) => {
            ctx.set(rootCtx, parent);
            ctx.set(defaultValueCtx, text);
            ctx.update(editorViewOptionsCtx, (options) => ({
                ...options,
                editable,
                attributes,
                nodeViews: { list_item: taskItemView(editable) as NodeViewConstructor, front_matter: propertiesView(onProperties) },
                // A dropped file is not inserted.
                handleDrop: (view, event) => Boolean(event.dataTransfer && event.dataTransfer.files.length > 0),
            }));
        })
        // An empty paragraph is not written as raw HTML.
        .use(commonmark.filter((plugin) => !remarkPreserveEmptyLinePlugin.includes(plugin)))
        .use(gfm)
        .use(frontMatterPlugins);
    if (withHistory) editor = editor.use(history);
    editor = editor
        .use(clipboard)
        .use(links)
        .use(images)
        .use(html)
        .use(headings);
    for (const plugin of [keepFrontMatter(editable), ...plugins]) editor = editor.use($prose(() => plugin));
    return editor.create();
}

// The matches of query in doc as [from, to] positions. A match stays within one block; an image, raw HTML or a line
// break inside a block is a character no query matches. match(text, query) answers the matches in the text of one block.
export function matchesIn(doc: ProseNode, query: string, match: (text: string, query: string) => Match[]) {
    const found: Match[] = [];
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
export function reveal(view: EditorView, scroller: HTMLElement, from: number, to: number) {
    const { node } = view.domAtPos(from);
    const block = (node.nodeType === Node.ELEMENT_NODE ? node as Element : node.parentElement)?.closest('pre, .tableWrapper');
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
