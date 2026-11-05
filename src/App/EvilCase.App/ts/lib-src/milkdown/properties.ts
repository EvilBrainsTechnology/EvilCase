// The YAML front matter of a Markdown text: the first block of the document, shown as its properties and written back
// as it stood in the file. Its YAML is read only to show it; it never changes in a formatted view.
import remarkFrontmatter from 'remark-frontmatter';
import { isAlias, isMap, isScalar, isSeq, parseDocument } from 'yaml';
import { docSchema } from '@milkdown/kit/preset/commonmark';
import { $node, $remark } from '@milkdown/kit/utils';
import { Plugin } from '@milkdown/kit/prose/state';
import type { MilkdownPlugin } from '@milkdown/kit/ctx';
import type { NodeSchema } from '@milkdown/kit/transformer';
import type { DOMOutputSpec, Node as ProseNode, NodeSpec, Schema } from '@milkdown/kit/prose/model';
import type { EditorView, NodeView, ViewMutationRecord } from '@milkdown/kit/prose/view';

// A property as it shows: its key and its value, a text, a list or properties of their own.
export interface Property {
    key: string;
    value: PropertyValue;
}

export type PropertyValue = { text: string } | { items: PropertyValue[] } | { properties: Property[] };

// Values nested deeper show as their YAML text.
const deepest = 8;

// A scalar shows as it is written in the file, a quoted or block one as its text; null shows empty.
function textOf(node: unknown, source: string): string {
    if (isScalar(node)) return node.value === null ? '' : (node.source ?? String(node.value)).replace(/\n+$/, '');
    if (isAlias(node)) return '*' + node.source;
    if ((isMap(node) || isSeq(node)) && node.range) return source.slice(node.range[0], node.range[1]).trimEnd();
    return '';
}

function valueOf(node: unknown, source: string, depth: number): PropertyValue {
    if (depth < deepest && isMap(node) && node.items.length > 0) return { properties: propertiesIn(node.items, source, depth + 1) };
    if (depth < deepest && isSeq(node) && node.items.length > 0) return { items: node.items.map((item) => valueOf(item, source, depth + 1)) };
    if ((isMap(node) || isSeq(node)) && node.items.length === 0) return { text: '' };
    return { text: textOf(node, source) };
}

function propertiesIn(pairs: { key: unknown; value: unknown }[], source: string, depth: number): Property[] {
    return pairs.map((pair) => ({ key: textOf(pair.key, source), value: valueOf(pair.value, source, depth) }));
}

// The properties of the YAML text source in the order of the file, none for an empty one; null where it is not valid
// YAML or not a mapping.
export function propertiesOf(source: string): Property[] | null {
    try {
        const document = parseDocument(source);
        if (document.errors.length > 0) return null;
        if (document.contents === null) return [];
        return isMap(document.contents) ? propertiesIn(document.contents.items, source, 1) : null;
    } catch {
        return null;
    }
}

// The properties of source as the content of a front matter node: a table of them, or its text where it is not valid
// YAML.
function propertiesNode(schema: Schema, source: string): ProseNode {
    const { nodes } = schema;
    const text = (value: string) => (value ? [schema.text(value)] : []);
    const table = (properties: Property[]): ProseNode => nodes.property_table.create(null, properties.map((property) => nodes.property_row.create(null, [
        nodes.property_key.create(null, text(property.key)),
        nodes.property_value.create(null, value(property.value)),
    ])));
    const value = (shown: PropertyValue): ProseNode => {
        if ('text' in shown) return nodes.property_text.create(null, text(shown.text));
        if ('properties' in shown) return table(shown.properties);
        return nodes.property_list.create({ tables: shown.items.every((item) => 'properties' in item) }, shown.items.map((item) => nodes.property_item.create(null, value(item))));
    };
    const properties = propertiesOf(source);
    return properties ? table(properties) : nodes.property_raw.create(null, text(source));
}

// A part of the properties comes only from the YAML of the front matter: never from Markdown or pasted HTML, and it is
// never written itself.
function part(id: string, spec: NodeSpec) {
    return $node(id, (): NodeSchema => ({
        ...spec,
        parseDOM: [],
        parseMarkdown: { match: () => false, runner: () => {} },
        toMarkdown: { match: () => false, runner: () => {} },
    }));
}

const textual = { content: 'text*', marks: '' };
const cell = 'property_text | property_list | property_table';

// The document may start with the front matter.
const doc = $node('doc', () => ({ ...docSchema.schema, content: 'front_matter? block+' }));

const frontMatter = $node('front_matter', (): NodeSchema => ({
    atom: true,
    selectable: false,
    isolating: true,
    defining: true,
    content: 'property_table | property_raw',
    attrs: { value: { default: '', validate: 'string' } },
    parseDOM: [],
    toDOM: (): DOMOutputSpec => ['section', { class: 'ec-markdown-properties' },
        ['div', { class: 'ec-markdown-properties-header' }, 'Vlastnosti'], ['div', { class: 'ec-markdown-properties-body' }, 0]],
    parseMarkdown: {
        match: ({ type }) => type === 'yaml',
        runner: (state, node, type) => {
            // The editor joins lines with "\n", as a save expects.
            const value = typeof node.value === 'string' ? node.value.replace(/\r\n?/g, '\n') : '';
            state.addNode(type, { value }, [propertiesNode(type.schema, value)]);
        },
    },
    toMarkdown: {
        match: (node) => node.type.name === 'front_matter',
        runner: (state, node) => {
            state.addNode('yaml', undefined, node.attrs.value);
        },
    },
}));

// The schema and parsing of the front matter for every Markdown view.
export const frontMatterPlugins: MilkdownPlugin[] = [
    ...$remark('remarkFrontmatter', () => remarkFrontmatter, 'yaml'),
    doc,
    frontMatter,
    part('property_table', { content: 'property_row*', toDOM: () => ['table', { class: 'ec-markdown-property-table' }, ['tbody', 0]] }),
    part('property_row', { content: 'property_key property_value', toDOM: () => ['tr', 0] }),
    part('property_key', { ...textual, toDOM: () => ['th', { scope: 'row' }, 0] }),
    part('property_value', { content: cell, toDOM: () => ['td', 0] }),
    part('property_text', { ...textual, toDOM: () => ['div', { class: 'ec-markdown-property-text' }, 0] }),
    part('property_list', {
        content: 'property_item*',
        attrs: { tables: { default: false } },
        toDOM: (node) => ['ul', { class: node.attrs.tables ? 'ec-markdown-property-tables' : 'ec-markdown-property-list' }, 0],
    }),
    part('property_item', { content: cell, toDOM: () => ['li', 0] }),
    part('property_raw', {
        ...textual,
        code: true,
        toDOM: () => ['div', { class: 'ec-markdown-properties-invalid' },
            ['p', { class: 'ec-markdown-properties-note' }, 'Vlastnosti nejsou platný YAML.'], ['pre', ['code', 0]]],
    }),
];

function frontMatterOf(doc: ProseNode) {
    return doc.firstChild?.type.name === 'front_matter' ? doc.firstChild : null;
}

// While editable() the front matter stays as it was: a change that removes, replaces or alters it is undone in the
// same step.
export function keepFrontMatter(editable: () => boolean) {
    return new Plugin({
        appendTransaction(transactions, before, after) {
            if (!editable() || !transactions.some((tr) => tr.docChanged)) return null;
            const was = frontMatterOf(before.doc);
            const now = frontMatterOf(after.doc);
            if (was === now || (was && now && was.eq(now))) return null;
            if (!now) return after.tr.insert(0, was!);
            return was ? after.tr.replaceWith(0, now.nodeSize, was) : after.tr.delete(0, now.nodeSize);
        },
    });
}

// How a shown front matter opens and closes, by its element.
const shown = new WeakMap<Node, (open: boolean) => void>();
let views = 0;

function span(className: string, text?: string) {
    const dom = document.createElement('span');
    dom.className = className;
    if (text !== undefined) dom.textContent = text;
    return dom;
}

// The front matter: a button that names the properties and their count and opens or closes them, closed on every
// opening. onToggle(open) hears the button.
export function propertiesView(onToggle?: (open: boolean) => void) {
    return (node: ProseNode): NodeView => {
        let current = node;
        const dom = document.createElement('section');
        const toggle = document.createElement('button');
        const count = span('ec-markdown-properties-count');
        const contentDOM = document.createElement('div');
        dom.className = 'ec-markdown-properties';
        dom.contentEditable = 'false';
        toggle.type = 'button';
        toggle.className = 'ec-markdown-properties-toggle';
        toggle.append(span('ec-markdown-properties-chevron'), span('ec-markdown-properties-title', 'Vlastnosti'), count);
        contentDOM.className = 'ec-markdown-properties-body';
        contentDOM.id = 'ec-markdown-properties-' + ++views;
        toggle.setAttribute('aria-controls', contentDOM.id);
        dom.append(toggle, contentDOM);
        let opened = false;
        const open = (value: boolean) => {
            opened = value;
            toggle.setAttribute('aria-expanded', String(value));
            contentDOM.hidden = !value;
        };
        const paint = () => {
            const table = current.firstChild?.type.name === 'property_table' ? current.firstChild : null;
            count.textContent = table ? String(table.childCount) : '';
            count.hidden = !table;
        };
        toggle.addEventListener('click', () => {
            open(!opened);
            onToggle?.(opened);
        });
        open(false);
        paint();
        shown.set(dom, open);
        return {
            dom,
            contentDOM,
            update: (next: ProseNode) => {
                if (next.type !== current.type) return false;
                current = next;
                paint();
                return true;
            },
            stopEvent: (event: Event) => toggle.contains(event.target as Node),
            ignoreMutation: (mutation: ViewMutationRecord) => mutation.type !== 'selection'
                && (mutation.target === contentDOM ? mutation.type === 'attributes' : !contentDOM.contains(mutation.target)),
        };
    };
}

// The end of the front matter of the document in view, 0 where it has none.
export function propertiesEnd(view: EditorView) {
    return frontMatterOf(view.state.doc)?.nodeSize ?? 0;
}

// Opens or closes the front matter of the document in view.
export function showProperties(view: EditorView, open: boolean) {
    if (propertiesEnd(view) > 0) shown.get(view.nodeDOM(0)!)?.(open);
}
