// The differences of two formatted Markdown texts as the compare shows them, read from the blocks of each document in
// order (blocksOf of the Markdown library). It needs no browser.

const markWords = [['strong', 'tučně'], ['emphasis', 'kurzívou'], ['strike_through', 'přeškrtnuto'], ['inlineCode', 'jako kód']];
const itemNames = { bullet: 'odrážka', ordered: 'číslovaná položka', task: 'úkol', done: 'splněný úkol' };

// What a block is, as a change of its type names it: "nadpis 2", "citace, odrážka", "odstavec".
function blockName(block) {
    const parts = [];
    if (block.chain.some((entry) => entry.type === 'blockquote')) parts.push('citace');
    const items = block.chain.filter((entry) => entry.type === 'list_item');
    if (items.length > 0) {
        const name = itemNames[items.at(-1).kind];
        parts.push(items.length > 1 ? name + ' ' + items.length + '. úrovně' : name);
    }
    if (block.type === 'heading') parts.push('nadpis ' + block.level);
    else if (block.type === 'code_block') parts.push('blok kódu');
    else if (block.type === 'hr') parts.push('oddělovač');
    else if (parts.length === 0) parts.push('odstavec');
    return parts.join(', ');
}

// How the marks of a stretch of text changed: "nově tučně, dříve kurzívou"; empty where they did not.
function formatChange(was, now) {
    const parts = [];
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

function marksAt(block, offset) {
    return block.runs.find(([from, to]) => from <= offset && offset < to)?.[2] ?? {};
}

// The stretches of text the changes leave alike, as [fromA, fromB, length].
function kept(changes, length) {
    const stretches = [];
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
function indexAt(starts, offset) {
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
function align(keysA, keysB, diff) {
    const codes = new Map();
    const code = (key) => {
        if (!codes.has(key)) {
            const next = codes.size + 1;
            codes.set(key, String.fromCharCode(next < 0xd800 ? next : next + 0x800));
        }
        return codes.get(key);
    };
    const textA = keysA.map(code).join('');
    const textB = keysB.map(code).join('');
    const steps = [];
    let a = 0;
    let b = 0;
    const gap = (toA, toB) => {
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
function pairGap(keysA, keysB, diff) {
    const steps = [];
    if (keysA.length === 0 || keysB.length === 0) {
        keysA.forEach((key, i) => steps.push([i, -1]));
        keysB.forEach((key, j) => steps.push([-1, j]));
        return steps;
    }
    const join = (keys) => keys.map((key) => key.replaceAll('\n', ' ') + '\n').join('');
    const startsOf = (keys) => {
        const starts = [];
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
    const shared = keysA.map(() => new Map());

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

    const bestA = new Map();
    shared.forEach((counts, i) => {
        for (const [j, count] of counts) {
            if (!bestA.has(j) || count > bestA.get(j)[1]) bestA.set(j, [i, count]);
        }
    });

    let next = 0;
    shared.forEach((counts, i) => {
        let j = -1;
        let most = 0;
        for (const [candidate, count] of counts) {
            if (count > most) [j, most] = [candidate, count];
        }
        if (j < next || bestA.get(j)[0] !== i) {
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
function following(steps, items) {
    const found = new Array(steps.length);
    for (let index = steps.length - 1, next = null; index >= 0; index--) {
        found[index] = next;
        if (steps[index][1] >= 0) next = items[steps[index][1]];
    }
    return found;
}

// The format changes of the stretch both blocks keep alike: ranges of the newer with the change named. Answers the names.
function restyle(a, b, fromA, fromB, length, out) {
    const cuts = new Set([0, length]);
    for (const [block, from] of [[a, fromA], [b, fromB]]) {
        for (const [start, end] of block.runs) {
            for (const at of [start - from, end - from]) {
                if (at > 0 && at < length) cuts.add(at);
            }
        }
    }
    const sorted = [...cuts].sort((first, second) => first - second);
    const titles = [];
    let last = null;
    for (let index = 0; index + 1 < sorted.length; index++) {
        const title = formatChange(marksAt(a, fromA + sorted[index]), marksAt(b, fromB + sorted[index]));
        const from = b.pos + fromB + sorted[index];
        const to = b.pos + fromB + sorted[index + 1];
        if (!title) {
            last = null;
        } else if (last && last.to === from && last.title === title) {
            last.to = to;
        } else {
            last = { from, to, type: 'format', title };
            out.newer.push(last);
            titles.push(title);
        }
    }
    return titles;
}

// Compares two text blocks that face each other; typed compares their types too. The newer block names its changes of
// type and format on a label. Answers whether they differ.
function compareBlocks(a, b, out, diff, typed) {
    const changes = a.text === b.text ? [] : diff(a.text, b.text);
    for (const change of changes) {
        if (change.toA > change.fromA) {
            out.older.push({ from: a.pos + change.fromA, to: a.pos + change.toA, type: 'deleted' });
            out.deletions.push({ at: b.pos + change.fromB, from: a.pos + change.fromA, to: a.pos + change.toA });
        }
        if (change.toB > change.fromB) out.newer.push({ from: b.pos + change.fromB, to: b.pos + change.toB, type: 'inserted' });
    }
    const names = new Set();
    if (typed && blockName(a) !== blockName(b)) {
        out.newer.push({ node: b.node, end: b.end, type: 'restyled' });
        names.add(blockName(a) + ' → ' + blockName(b));
    }
    for (const [fromA, fromB, length] of kept(changes, a.text.length)) {
        for (const title of restyle(a, b, fromA, fromB, length, out)) names.add(title);
    }
    if (names.size > 0) out.newer.push({ node: b.node, end: b.end, type: 'named', title: [...names].join('; ') });
    return changes.length > 0 || names.size > 0;
}

// Where a removed block stands in the newer document: before the block that follows it, inside the blocks around both,
// and wrapped in the blocks around it that the newer lacks there. A list item is never shared.
function place(block, next, size) {
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
function compareTables(a, b, out, diff) {
    const first = (row) => row.cells[0]?.block.text ?? '';
    const byFirst = [a, b].every((table) => new Set(table.rows.map(first)).size === table.rows.length);
    const key = (row) => (byFirst ? first(row) : row.cells.map((cell) => cell.block.text).join('\t'));
    const steps = align(a.rows.map(key), b.rows.map(key), diff);
    const after = following(steps, b.rows);
    let found = null;
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
            out.newer.push({ node: rowB.node, end: rowB.end, type: 'added' });
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

// The differences of newer against older ({ size, blocks } of blocksOf), diff(a, b) answering [{ fromA, toA, fromB, toB }].
// Answers the marks of each document (older, newer: { from, to } of text, { node, end } of a block, row or cell), the
// deletions one column shows at { at }, the facing blocks' pairs, the changes in order and the blocks added and removed.
export function diffMarkdown(older, newer, diff) {
    const out = { older: [], newer: [], deletions: [], pairs: [], changes: [], added: 0, removed: 0 };
    const key = (block) => (block.type === 'table' ? block.rows.map((row) => row.cells.map((cell) => cell.block.text).join(' ')).join(' ') : block.text);
    const steps = align(older.blocks.map(key), newer.blocks.map(key), diff);
    const after = following(steps, newer.blocks);
    let change = null;

    steps.forEach(([i, j], index) => {
        const a = i >= 0 ? older.blocks[i] : null;
        const b = j >= 0 ? newer.blocks[j] : null;
        let found = null;

        if (a && b && (a.type === 'table') === (b.type === 'table')) {
            out.pairs.push([a.node, b.node]);
            if (a.type === 'table') {
                found = compareTables(a, b, out, diff);
            } else if (compareBlocks(a, b, out, diff, true)) {
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
                out.removed += a.type === 'table' ? a.rows.length : 1;
            }
            if (b) {
                out.newer.push({ node: b.node, end: b.end, type: 'added' });
                out.added += b.type === 'table' ? b.rows.length : 1;
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
