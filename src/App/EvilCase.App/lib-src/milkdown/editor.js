// The Markdown view and editor with its toolbar commands and search.
import { commandsCtx, editorViewCtx, serializerCtx } from '@milkdown/kit/core';
import { insertTableCommand } from '@milkdown/kit/preset/gfm';
import { replaceAll } from '@milkdown/kit/utils';
import { Plugin, PluginKey, TextSelection } from '@milkdown/kit/prose/state';
import { Decoration, DecorationSet } from '@milkdown/kit/prose/view';
import { keymap } from '@milkdown/kit/prose/keymap';
import { redo, undo, undoDepth } from '@milkdown/kit/prose/history';
import { checks, code, heading, linkAt, list, mark, markBox, markdownEditor, matchesIn, quote, reveal, safeLink, within } from './shared.js';

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
