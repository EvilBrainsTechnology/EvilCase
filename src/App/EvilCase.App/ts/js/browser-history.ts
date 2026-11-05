// The browser's latest steps through its history, so that a Back or Forward the app held back can be taken later.
// _index is the position Blazor keeps in each history entry's state.
interface Step {
    index: number;
    href: string;
}

interface BlazorHistoryState {
    _index?: number;
}

const steps: Step[] = [];
let tracking = false;

function remember(event: PopStateEvent) {
    steps.push({ index: (event.state as BlazorHistoryState | null)?._index ?? 0, href: location.href });
    if (steps.length > 4) steps.shift();
}

export function trackSteps() {
    steps.length = 0;
    if (tracking) return;
    tracking = true;
    window.addEventListener('popstate', remember);
}

// The distance to the entry of the latest step to target, 0 when none is known; forgets every step.
export function takeStep(target: string) {
    const href = new URL(target, document.baseURI).href;
    const current = (history.state as BlazorHistoryState | null)?._index ?? 0;
    const step = steps.findLast((candidate) => candidate.href === href && candidate.index !== current);
    steps.length = 0;
    return step ? step.index - current : 0;
}

export function go(distance: number) {
    history.go(distance);
}
