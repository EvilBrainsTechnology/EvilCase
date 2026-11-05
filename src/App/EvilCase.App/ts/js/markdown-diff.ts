// The differences of two formatted Markdown texts as the compare shows them, read from the blocks of each document in
// order (blocksOf of the Markdown library). It needs no browser.
import type { Block, Blocks, ChainEntry, Change, DifferenceMark, Differences, Marks, PropertiesBlock, PropertyEntry, TableBlock, TableRow, TextBlock } from '../lib-src/milkdown/compare.js';

// A change of b against a, as the text editor's library diffs two texts.
interface TextChange {
    fromA: number;
    toA: number;
    fromB: number;
    toB: number;
}

// diff(a, b, exact) of the text editor's library.
export type DiffTexts = (a: string, b: string, exact?: boolean) => readonly TextChange[];

// [index in a, index in b] of two items that face each other, -1 for the side that has none.
type Step = [number, number];

type FormatMark = { from: number; to: number; type: 'format'; title: string };

const markWords: [string, string][] = [['strong', 'tučně'], ['emphasis', 'kurzívou'], ['strike_through', 'přeškrtnuto'], ['inlineCode', 'jako kód']];
const itemNames: Record<NonNullable<ChainEntry['kind']>, string> = { bullet: 'odrážka', ordered: 'číslovaná položka', task: 'úkol', done: 'splněný úkol' };

// What a block is, as a change of its type names it: "nadpis 2", "citace, odrážka", "odstavec".
function blockName(block: TextBlock) {
    const parts: string[] = [];
    if (block.chain.some((entry) => entry.type === 'blockquote')) parts.push('citace');
    const items = block.chain.filter((entry) => entry.type === 'list_item');
    if (items.length > 0) {
        const name = itemNames[items.at(-1)!.kind!];
        parts.push(items.length > 1 ? name + ' ' + items.length + '. úrovně' : name);
    }
    if (block.type === 'heading') parts.push('nadpis ' + block.level);
    else if (block.type === 'code_block') parts.push('blok kódu');
    else if (block.type === 'hr') parts.push('oddělovač');
    else if (parts.length === 0) parts.push('odstavec');
    return parts.join(', ');
}

// How the marks of a stretch of text changed: "nově tučně, dříve kurzívou"; empty where they did not.
function formatChange(was: Marks, now: Marks) {
    const parts: string[] = [];
    for (const [name, word] of markWords) {
        if (!was[name] && now[name]) parts.push('nově ' + word);
        else if (was[name] && !now[name]) parts.push('dříve ' + word);
    }
    if (was.link !== now.link) {
        if (!was.link) parts.push('nově odkaz na ' + now.link);
        else if (!now.link) parts.push('dříve odkaz na ' + was.link);
        else parts.push('adresa odkazu ' + was.link + ' → ' + now.link);
    }
    return parts.join(', ');
}

function marksAt(block: TextBlock, offset: number): Marks {
    return block.runs.find(([from, to]) => from <= offset && offset < to)?.[2] ?? {};
}

// The stretches of text the changes leave alike, as [fromA, fromB, length].
function kept(changes: readonly TextChange[], length: number) {
    const stretches: [number, number, number][] = [];
    let a = 0;
    let b = 0;
    for (const change of changes) {
        if (change.fromA > a) stretches.push([a, b, change.fromA - a]);
        a = change.toA;
        b = change.toB;
    }
    if (length > a) stretches.push([a, b, length - a]);
    return stretches;
}

// The index of the last start at or before offset.
function indexAt(starts: number[], offset: number) {
    let low = 0;
    let high = starts.length;
    while (low < high) {
        const middle = (low + high) >> 1;
        if (starts[middle] <= offset) low = middle + 1; else high = middle;
    }
    return low - 1;
}

// How two lists of item texts face each other in order: [a, b], -1 for an item only one side has. Equal items pair
// first; between them, items pair with the item they share the most text with on both sides.
function align(keysA: string[], keysB: string[], diff: DiffTexts) {
    const codes = new Map<string, string>();
    const code = (key: string) => {
        if (!codes.has(key)) {
            const next = codes.size + 1;
            codes.set(key, String.fromCharCode(next < 0xd800 ? next : next + 0x800));
        }
        return codes.get(key)!;
    };
    const textA = keysA.map(code).join('');
    const textB = keysB.map(code).join('');
    const steps: Step[] = [];
    let a = 0;
    let b = 0;
    const gap = (toA: number, toB: number) => {
        for (const [i, j] of pairGap(keysA.slice(a, toA), keysB.slice(b, toB), diff)) {
            steps.push([i < 0 ? -1 : a + i, j < 0 ? -1 : b + j]);
        }
    };
    const equal = codes.size > 0xf7ff ? [] : kept(diff(textA, textB, true), textA.length);
    for (const [fromA, fromB, length] of equal) {
        gap(fromA, fromB);
        for (let index = 0; index < length; index++) steps.push([fromA + index, fromB + index]);
        a = fromA + length;
        b = fromB + length;
    }
    gap(keysA.length, keysB.length);
    return steps;
}

// align for the items between two equal ones.
function pairGap(keysA: string[], keysB: string[], diff: DiffTexts) {
    const steps: Step[] = [];
    if (keysA.length === 0 || keysB.length === 0) {
        keysA.forEach((key, i) => steps.push([i, -1]));
        keysB.forEach((key, j) => steps.push([-1, j]));
        return steps;
    }
    const join = (keys: string[]) => keys.map((key) => key.replaceAll('\n', ' ') + '\n').join('');
    const startsOf = (keys: string[]) => {
        const starts: number[] = [];
        let at = 0;
        for (const key of keys) {
            starts.push(at);
            at += key.length + 1;
        }
        return starts;
    };
    const textA = join(keysA);
    const startsA = startsOf(keysA);
    const startsB = startsOf(keysB);
    const shared = keysA.map(() => new Map<number, number>());

    for (const [fromA, fromB, length] of kept(diff(textA, join(keysB)), textA.length)) {
        const shift = fromB - fromA;
        for (let a = fromA; a < fromA + length;) {
            const i = indexAt(startsA, a);
            const j = indexAt(startsB, a + shift);
            const end = Math.min(fromA + length, startsA[i] + keysA[i].length + 1, startsB[j] + keysB[j].length + 1 - shift);
            const alike = Math.min(end, startsA[i] + keysA[i].length) - a + (keysA[i].length === 0 && keysB[j].length === 0 ? 1 : 0);
            if (alike > 0) shared[i].set(j, (shared[i].get(j) ?? 0) + alike);
            a = end;
        }
    }

    const bestA = new Map<number, [number, number]>();
    shared.forEach((counts, i) => {
        for (const [j, count] of counts) {
            if (!bestA.has(j) || count > bestA.get(j)![1]) bestA.set(j, [i, count]);
        }
    });

    let next = 0;
    shared.forEach((counts, i) => {
        let j = -1;
        let most = 0;
        for (const [candidate, count] of counts) {
            if (count > most) [j, most] = [candidate, count];
        }
        if (j < next || bestA.get(j)![0] !== i) {
            steps.push([i, -1]);
            return;
        }
        for (; next < j; next++) steps.push([-1, next]);
        steps.push([i, j]);
        next = j + 1;
    });
    for (; next < keysB.length; next++) steps.push([-1, next]);
    return steps;
}

// For each step, the newer item of the first step after it that has one.
function following<T>(steps: Step[], items: T[]) {
    const found = new Array<T | null>(steps.length);
    for (let index = steps.length - 1, next = null as T | null; index >= 0; index--) {
        found[index] = next;
        if (steps[index][1] >= 0) next = items[steps[index][1]];
    }
    return found;
}

// The format changes of the stretch both blocks keep alike: ranges of the newer with the change named, after the
// block's change of type where it has one.
function restyle(a: TextBlock, b: TextBlock, fromA: number, fromB: number, length: number, type: string) {
    const cuts = new Set([0, length]);
    for (const [block, from] of [[a, fromA], [b, fromB]] as [TextBlock, number][]) {
        for (const [start, end] of block.runs) {
            for (const at of [start - from, end - from]) {
                if (at > 0 && at < length) cuts.add(at);
            }
        }
    }
    const sorted = [...cuts].sort((first, second) => first - second);
    const ranges: FormatMark[] = [];
    let last: FormatMark | null = null;
    for (let index = 0; index + 1 < sorted.length; index++) {
        const change = formatChange(marksAt(a, fromA + sorted[index]), marksAt(b, fromB + sorted[index]));
        const title = change && type ? type + '; ' + change : change;
        const from = b.pos + fromB + sorted[index];
        const to = b.pos + fromB + sorted[index + 1];
        if (!title) {
            last = null;
        } else if (last && last.to === from && last.title === title) {
            last.to = to;
        } else {
            last = { from, to, type: 'format', title };
            ranges.push(last);
        }
    }
    return ranges;
}

// Compares two text blocks that face each other; typed compares their types too. The newer block names a change of type
// over the text that no change of format covers. Answers whether they differ.
function compareBlocks(a: TextBlock, b: TextBlock, out: Differences, diff: DiffTexts, typed: boolean) {
    const changes = a.text === b.text ? [] : diff(a.text, b.text);
    for (const change of changes) {
        if (change.toA > change.fromA) {
            out.older.push({ from: a.pos + change.fromA, to: a.pos + change.toA, type: 'deleted' });
            out.deletions.push({ at: b.pos + change.fromB, from: a.pos + change.fromA, to: a.pos + change.toA });
        }
        if (change.toB > change.fromB) out.newer.push({ from: b.pos + change.fromB, to: b.pos + change.toB, type: 'inserted' });
    }
    const type = typed && blockName(a) !== blockName(b) ? blockName(a) + ' → ' + blockName(b) : '';
    const formats = kept(changes, a.text.length).flatMap(([fromA, fromB, length]) => restyle(a, b, fromA, fromB, length, type));
    if (type && b.text.length === 0) {
        out.newer.push({ node: b.node, end: b.end, type: 'restyled', title: type });
    } else if (type) {
        let at = b.pos;
        for (const { from, to } of [...formats, { from: b.pos + b.text.length }] as { from: number; to?: number }[]) {
            if (from > at) out.newer.push({ from: at, to: from, type: 'restyled', title: type });
            at = to!;
        }
    }
    out.newer.push(...formats);
    return changes.length > 0 || type !== '' || formats.length > 0;
}

// Where a removed block stands in the newer document: before the block that follows it, inside the blocks around both,
// and wrapped in the blocks around it that the newer lacks there. A list item is never shared.
function place(block: Block, next: Block | null, size: number) {
    if (!next) return { at: size, wrap: block.chain.map((entry) => entry.pos) };
    let shared = 0;
    while (shared < block.chain.length && shared < next.chain.length && block.chain[shared].type === next.chain[shared].type
        && block.chain[shared].type !== 'list_item') {
        shared++;
    }
    return { at: shared < next.chain.length ? next.chain[shared].pos : next.node, wrap: block.chain.slice(shared).map((entry) => entry.pos) };
}

// Compares two tables that face each other row by row, and the rows that face each other cell by cell. A row is known
// by its first cell where each table's first cells differ from each other, else by all its cells. Answers where the
// first difference stands, or null.
function compareTables(a: TableBlock, b: TableBlock, out: Differences, diff: DiffTexts) {
    const first = (row: TableRow) => row.cells[0]?.block.text ?? '';
    const byFirst = [a, b].every((table) => new Set(table.rows.map(first)).size === table.rows.length);
    const key = (row: TableRow) => (byFirst ? first(row) : row.cells.map((cell) => cell.block.text).join('\t'));
    const steps = align(a.rows.map(key), b.rows.map(key), diff);
    const after = following(steps, b.rows);
    let found: Change | null = null;
    steps.forEach(([i, j], index) => {
        const rowA = i >= 0 ? a.rows[i] : null;
        const rowB = j >= 0 ? b.rows[j] : null;
        let at = rowB?.node;
        let differs = true;
        if (rowA && rowB) {
            differs = false;
            for (let column = 0; column < Math.max(rowA.cells.length, rowB.cells.length); column++) {
                const cellA = rowA.cells[column];
                const cellB = rowB.cells[column];
                if (cellA && cellB) {
                    if (!compareBlocks(cellA.block, cellB.block, out, diff, false)) continue;
                    out.older.push({ node: cellA.node, end: cellA.end, type: 'changed' });
                    out.newer.push({ node: cellB.node, end: cellB.end, type: 'changed' });
                } else if (cellA) {
                    out.older.push({ node: cellA.node, end: cellA.end, type: 'removed' });
                    out.deletions.push({ at: rowB.end - 1, node: cellA.node, wrap: [] });
                } else {
                    out.newer.push({ node: cellB.node, end: cellB.end, type: 'added' });
                }
                differs = true;
            }
            if (differs) {
                out.added++;
                out.removed++;
            }
        } else if (rowA) {
            at = after[index]?.node ?? b.end - 1;
            out.older.push({ node: rowA.node, end: rowA.end, type: 'removed' });
            out.deletions.push({ at, node: rowA.node, wrap: [] });
            out.removed++;
        } else {
            out.newer.push({ node: rowB!.node, end: rowB!.end, type: 'added' });
            out.added++;
        }
        if (!differs) return;
        if (!found) {
            found = { older: rowA?.node ?? null, newer: rowB?.node ?? null, at };
        } else {
            found.older ??= rowA?.node ?? null;
            found.newer ??= rowB?.node ?? null;
        }
    });
    return found;
}

// Compares two front matters property by property, a property known by its key; each property that differs is a change
// of its own and counts as a block on each side it stands on. A text value is compared word by word, any other value
// whole. A front matter that is not valid YAML compares as text.
function compareProperties(a: PropertiesBlock, b: PropertiesBlock, out: Differences, diff: DiffTexts) {
    const changed = (older: { node: number; end: number }, newer: { node: number; end: number }) => {
        out.older.push({ node: older.node, end: older.end, type: 'changed' });
        out.newer.push({ node: newer.node, end: newer.end, type: 'changed' });
        out.changes.push({ older: older.node, newer: newer.node, at: newer.node });
        out.added++;
        out.removed++;
    };
    if (a.raw && b.raw) {
        if (compareBlocks(a.raw, b.raw, out, diff, false)) changed(a, b);
        return;
    }
    if (a.raw || b.raw) {
        out.deletions.push({ at: b.node, node: a.node, wrap: [] });
        changed(a, b);
        return;
    }
    const keys = new Map<string, number>();
    a.entries.forEach((entry, index) => {
        if (!keys.has(entry.key)) keys.set(entry.key, index);
    });
    const kept = new Set(b.entries.map((entry) => entry.key));
    let next = 0;
    // The removed properties before index of the older stand before at in the newer.
    const removeUntil = (index: number, at: number) => {
        for (; next < index; next++) {
            const entry = a.entries[next];
            if (kept.has(entry.key)) continue;
            out.older.push({ node: entry.node, end: entry.end, type: 'removed' });
            out.deletions.push({ at, node: entry.node, wrap: [] });
            out.changes.push({ older: entry.node, newer: null, at });
            out.removed++;
        }
    };
    const compare = (older: PropertyEntry, newer: PropertyEntry) => {
        if (older.value === newer.value) return;
        if (older.text && newer.text) {
            compareBlocks(older.text, newer.text, out, diff, false);
        } else {
            out.deletions.push({ at: newer.node, node: older.node, wrap: [] });
        }
        changed(older, newer);
    };
    for (const entry of b.entries) {
        const index = keys.get(entry.key);
        if (index === undefined) {
            out.newer.push({ node: entry.node, end: entry.end, type: 'added' });
            out.changes.push({ older: null, newer: entry.node, at: entry.node });
            out.added++;
            continue;
        }
        keys.delete(entry.key);
        if (index >= next) {
            removeUntil(index, entry.node);
            next = index + 1;
        }
        compare(a.entries[index], entry);
    }
    removeUntil(a.entries.length, b.table);
}

// The differences of newer against older ({ size, blocks } of blocksOf), diff(a, b) answering [{ fromA, toA, fromB, toB }].
// Answers the marks of each document (older, newer: { from, to } of text, { node, end } of a block, row or cell), the
// deletions one column shows at { at }, the facing blocks' pairs, the changes in order and the blocks added and removed.
export function diffMarkdown(older: Blocks, newer: Blocks, diff: DiffTexts) {
    const out: Differences = { older: [], newer: [], deletions: [], pairs: [], changes: [], added: 0, removed: 0 };
    // The front matters face each other.
    const key = (block: Block) => {
        if (block.type === 'properties') return '\uFFFF';
        return block.type === 'table' ? (block as TableBlock).rows.map((row) => row.cells.map((cell) => cell.block.text).join(' ')).join(' ') : (block as TextBlock).text;
    };
    const kind = (block: Block) => (block.type === 'table' || block.type === 'properties' ? block.type : 'text');
    const count = (block: Block) => {
        if (block.type === 'table') return (block as TableBlock).rows.length;
        return block.type === 'properties' ? Math.max((block as PropertiesBlock).entries.length, 1) : 1;
    };
    const steps = align(older.blocks.map(key), newer.blocks.map(key), diff);
    const after = following(steps, newer.blocks);
    let change: Change | null = null;

    steps.forEach(([i, j], index) => {
        const a = i >= 0 ? older.blocks[i] : null;
        const b = j >= 0 ? newer.blocks[j] : null;
        let found: Change | null = null;

        if (a && b && kind(a) === kind(b)) {
            out.pairs.push([a.node, b.node]);
            if (a.type === 'table') {
                found = compareTables(a as TableBlock, b as TableBlock, out, diff);
            } else if (a.type === 'properties') {
                compareProperties(a as PropertiesBlock, b as PropertiesBlock, out, diff);
            } else if (compareBlocks(a as TextBlock, b as TextBlock, out, diff, true)) {
                found = { older: a.node, newer: b.node, at: b.node };
                out.added++;
                out.removed++;
            }
        } else {
            found = { older: a?.node ?? null, newer: b?.node ?? null, at: b?.node };
            if (a) {
                const { at, wrap } = place(a, b ?? after[index], newer.size);
                found.at ??= at;
                out.older.push({ node: a.node, end: a.end, type: 'removed' });
                out.deletions.push({ at, node: a.node, wrap });
                out.removed += count(a);
            }
            if (b) {
                out.newer.push({ node: b.node, end: b.end, type: 'added' });
                out.added += count(b);
            }
        }

        if (!found) {
            change = null;
        } else if (!change) {
            change = found;
            out.changes.push(change);
        } else {
            change.older ??= found.older;
            change.newer ??= found.newer;
        }
    });

    return out;
}
