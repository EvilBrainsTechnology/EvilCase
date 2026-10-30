// The search of the previews: a match ignores case, diacritics and the kind of space.

// Case, diacritics and the kind of space make no difference to a match.
export function fold(text) {
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

// The folded text with, for each of its characters, the start and end offsets in text of the character it comes from.
export function foldText(text) {
    const parts = [];
    const starts = [];
    const ends = [];

    for (let offset = 0; offset < text.length;) {
        const size = text.codePointAt(offset) > 0xffff ? 2 : 1;
        const folded = fold(text.slice(offset, offset + size));

        for (let char = 0; char < folded.length; char++) {
            starts.push(offset);
            ends.push(offset + size);
        }

        parts.push(folded);
        offset += size;
    }

    return { text: parts.join(''), starts, ends };
}

// The matches of query in a text folded by foldText, as [from, to] offsets of the text, in order and never overlapping.
export function findMatches(folded, query) {
    const needle = fold(query);
    const matches = [];

    for (let start = needle.length > 0 ? folded.text.indexOf(needle) : -1; start >= 0; start = folded.text.indexOf(needle, start + needle.length)) {
        matches.push([folded.starts[start], folded.ends[start + needle.length - 1]]);
    }

    return matches;
}
