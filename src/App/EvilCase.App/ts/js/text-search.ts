// The search of the previews: a match ignores case, diacritics and the kind of space.

// A text folded by foldText: for each character of text, the start and end offsets of the character it comes from.
export interface FoldedText {
    text: string;
    starts: number[];
    ends: number[];
}

// [from, to] offsets of a text.
export type Match = [number, number];

// What debouncedFind keeps between calls.
export interface FindEntry {
    query: string;
    typing: number | null;
}

// Case, diacritics and the kind of space make no difference to a match.
export function fold(text: string) {
    let folded = '';

    for (const char of text) {
        const code = char.codePointAt(0)!;

        if (code < 0x80) {
            folded += code >= 65 && code <= 90 ? String.fromCharCode(code + 32) : code === 9 || code === 10 || code === 13 ? ' ' : char;
        } else {
            folded += /\s/.test(char) ? ' ' : char.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
        }
    }

    return folded;
}

// The folded text with, for each of its characters, the start and end offsets in text of the character it comes from.
export function foldText(text: string): FoldedText {
    const parts: string[] = [];
    const starts: number[] = [];
    const ends: number[] = [];

    for (let offset = 0; offset < text.length;) {
        const size = text.codePointAt(offset)! > 0xffff ? 2 : 1;
        const folded = fold(text.slice(offset, offset + size));

        for (let char = 0; char < folded.length; char++) {
            starts.push(offset);
            ends.push(offset + size);
        }

        // A mark that folds away belongs to the match of the character before it.
        if (folded.length === 0 && ends.length > 0) ends[ends.length - 1] = offset + size;

        parts.push(folded);
        offset += size;
    }

    return { text: parts.join(''), starts, ends };
}

// The matches of query in a text folded by foldText, as [from, to] offsets of the text, in order and never overlapping.
export function findMatches(folded: FoldedText, query: string) {
    const needle = fold(query);
    const matches: Match[] = [];

    for (let start = needle.length > 0 ? folded.text.indexOf(needle) : -1; start >= 0; start = folded.text.indexOf(needle, start + needle.length)) {
        matches.push([folded.starts[start], folded.ends[start + needle.length - 1]]);
    }

    return matches;
}

// An empty query clears the search at once; a new query searches once the typing pauses, or at once with again; again
// with the query searched last steps to the next match. entry keeps that query and the wait for the typing.
export function debouncedFind(entry: FindEntry, query: string, again: boolean, { clear, search, step }: { clear: () => void; search: () => void; step: () => void }) {
    clearTimeout(entry.typing ?? undefined);

    if (query.length === 0) {
        entry.query = '';
        clear();
    } else if (again && entry.query === query) {
        step();
    } else if (again) {
        entry.query = query;
        search();
    } else {
        entry.typing = setTimeout(() => {
            entry.query = query;
            search();
        }, 250);
    }
}
