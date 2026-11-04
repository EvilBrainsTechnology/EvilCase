// The app's one tooltip: a bubble with an arrow below the element marked data-tooltip, naming its value, or below the
// element marked data-tooltip-overflow, naming its whole text while the text is cut short. Where the bubble does not
// fit it flips or shifts into the viewport. A hover shows it after a delay, a keyboard focus at once; on an element
// also marked data-tooltip-tap any focus shows it, a tap included. index.html loads this module; it needs no call.
import { arrow, autoUpdate, computePosition, flip, hide, inline, offset, shift } from '../lib/floating-ui/floating-ui.min.js';

const hosts = '[data-tooltip], [data-tooltip-overflow]';
const hoverDelay = 450;

const bubble = document.createElement('div');
const label = document.createElement('span');
const tip = document.createElement('span');
// The ec class opens the tokens' scope outside the app's root.
bubble.className = 'ec ec-tooltip';
bubble.setAttribute('role', 'tooltip');
// A popover sits in the top layer, above an open dialog, and no overflow clips it.
bubble.popover = 'manual';
tip.className = 'ec-tooltip-arrow';
bubble.append(label, tip);
document.body.append(bubble);

let host = null;
let shownByFocus = false;
let pending = null;
let stopUpdates = null;
// Blazor removes a host without a pointerout or focusout.
const removal = new MutationObserver(() => {
    if (host && !host.isConnected) hideBubble();
});

function textOf(element) {
    if (element.hasAttribute('data-tooltip')) return element.getAttribute('data-tooltip');
    const cut = element.scrollWidth > element.clientWidth || element.scrollHeight > element.clientHeight;
    return cut ? element.textContent.trim() : '';
}

async function place(target, point) {
    // The arrow's length keeps the bubble off the host, the viewport edge and the bubble's corners.
    const length = tip.offsetWidth;
    const { x, y, placement, middlewareData } = await computePosition(target, bubble, {
        strategy: 'fixed',
        placement: 'bottom',
        middleware: [
            inline(point),
            offset(length),
            // Only a lack of room above or below flips the bubble; shift moves it along the host.
            flip({ crossAxis: false, fallbackPlacements: ['top', 'right', 'left'] }),
            shift({ padding: length }),
            arrow({ element: tip, padding: length }),
            hide(),
        ],
    });

    if (host !== target) return;

    bubble.dataset.side = placement;
    bubble.style.left = `${x}px`;
    bubble.style.top = `${y}px`;
    bubble.style.visibility = middlewareData.hide.referenceHidden ? 'hidden' : '';
    tip.style.left = middlewareData.arrow.x === undefined ? '' : `${middlewareData.arrow.x}px`;
    tip.style.top = middlewareData.arrow.y === undefined ? '' : `${middlewareData.arrow.y}px`;
}

function cancelPending() {
    clearTimeout(pending?.timer);
    pending = null;
}

function showBubble(target, byFocus, point) {
    cancelPending();
    if (target === host) {
        shownByFocus ||= byFocus;
        return;
    }

    hideBubble();
    const text = target.isConnected ? textOf(target) : '';
    if (!text) return;

    host = target;
    shownByFocus = byFocus;
    label.textContent = text;
    bubble.showPopover();
    removal.observe(document.body, { childList: true, subtree: true });
    stopUpdates = autoUpdate(target, bubble, () => place(target, point));
}

function hideBubble() {
    cancelPending();
    if (!host) return;

    host = null;
    stopUpdates();
    removal.disconnect();
    bubble.hidePopover();
}

function hostOf(event) {
    return event.target instanceof Element ? event.target.closest(hosts) : null;
}

// A touch shows no bubble on hover; a tap leaves the hover on.
document.addEventListener('pointerover', (event) => {
    const target = hostOf(event);
    if (event.pointerType === 'touch' || !target || target === host || target === pending?.host) return;

    const point = { x: event.clientX, y: event.clientY };
    if (host) {
        showBubble(target, false, point);
        return;
    }

    cancelPending();
    pending = { host: target, timer: setTimeout(() => showBubble(target, false, point), hoverDelay) };
});

document.addEventListener('pointerout', (event) => {
    const target = hostOf(event);
    if (!target || target.contains(event.relatedTarget)) return;

    if (target === pending?.host) cancelPending();
    if (target === host && !shownByFocus) hideBubble();
});

document.addEventListener('pointerdown', (event) => {
    if (!(host?.hasAttribute('data-tooltip-tap') && host.contains(event.target))) hideBubble();
}, true);

document.addEventListener('focusin', (event) => {
    const target = event.target;
    if (target instanceof Element && target.matches(hosts) && (target.matches(':focus-visible') || target.hasAttribute('data-tooltip-tap'))) {
        showBubble(target, true);
    }
});

document.addEventListener('focusout', (event) => {
    if (event.target === host) hideBubble();
});

document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') hideBubble();
});
