// navigator.clipboard exists only in a secure context; a rejection reaches .NET as a JSException.
export function writeText(text) {
    return navigator.clipboard.writeText(text);
}
