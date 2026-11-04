// The app's one tooltip bubble for [data-tooltip] and cut-short [data-tooltip-overflow] hosts; index.html loads it.
import { arrow, autoUpdate, computePosition, flip, hide, inline, offset, shift } from '../lib/floating-ui/floating-ui.min.js';

const hosts = '[data-tooltip], [data-tooltip-overflow]';
const hoverDelay = 450;
// A hover that reaches the next host within this time shows its bubble at once.
const handoff = 300;

const bubble = document.createElement('div');
const label = document.createElement('span');
const tip = document.createElement('span');
// The ec class opens the tokens' scope outside the app's root.
bubble.className = 'ec ec-tooltip';
bubble.id = 'ec-tooltip';
bubble.setAttribute('role', 'tooltip');
// A popover sits in the top layer, above an open dialog, and no overflow clips it.
bubble.popover = 'manual';
tip.className = 'ec-tooltip-arrow';
bubble.append(label, tip);
document.body.append(bubble);

let host = null;
let shownByFocus = false;
let describedBy = null;
let pending = null;
let stopUpdates = null;
let hiddenAt = 0;
let tabbing = false;
// A host clicked shows no bubble until the pointer leaves it.
let clicked = null;

// Blazor removes a host or changes its text without a pointerout or focusout.
const changes = new MutationObserver(() => {
    const text = host?.isConnected ? textOf(host) : '';
    if (!text) hideBubble();
    else if (text !== label.textContent) label.textContent = text;
});

function textOf(element) {
    if (element.hasAttribute('data-tooltip')) return element.getAttribute('data-tooltip') ?? '';

    // The laid-out text measures a fraction of a pixel that scrollWidth rounds away.
    const range = document.createRange();
    range.selectNodeContents(element);
    const cut = element.scrollHeight > element.clientHeight
        || range.getBoundingClientRect().width > element.getBoundingClientRect().width;
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
    // A cut-short text already reads whole to assistive technology.
    describedBy = target.getAttribute('aria-describedby');
    if (target.hasAttribute('data-tooltip')) target.setAttribute('aria-describedby', describedBy ? `${describedBy} ${bubble.id}` : bubble.id);
    // Inside a modal dialog the rest of the page is inert, so the bubble joins the dialog.
    (target.closest('dialog[open]') ?? document.body).append(bubble);
    bubble.showPopover();
    changes.observe(document.body, {
        childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['data-tooltip', 'data-tooltip-overflow'],
    });
    stopUpdates = autoUpdate(target, bubble, () => place(target, point));
}

function hideBubble() {
    cancelPending();
    if (!host) return;

    if (describedBy) host.setAttribute('aria-describedby', describedBy);
    else host.removeAttribute('aria-describedby');
    host = null;
    shownByFocus = false;
    stopUpdates();
    changes.disconnect();
    bubble.hidePopover();
    document.body.append(bubble);
}

function hostOf(event) {
    return event.target instanceof Element ? event.target.closest(hosts) : null;
}

// A touch shows no bubble on hover; a tap leaves the hover on. A hover never takes a keyboard focus's bubble.
document.addEventListener('pointerover', (event) => {
    const target = hostOf(event);
    if (event.pointerType === 'touch' || shownByFocus || !target || target === host || target === pending?.host || target === clicked) return;

    const point = { x: event.clientX, y: event.clientY };
    if (host || performance.now() - hiddenAt < handoff) {
        showBubble(target, false, point);
        return;
    }

    cancelPending();
    pending = { host: target, timer: setTimeout(() => showBubble(target, false, point), hoverDelay) };
});

document.addEventListener('pointerout', (event) => {
    const target = hostOf(event);
    if (!target || target.contains(event.relatedTarget)) return;

    if (target === clicked) clicked = null;
    if (target === pending?.host) cancelPending();
    if (target === host && !shownByFocus) {
        hideBubble();
        hiddenAt = performance.now();
    }
});

document.addEventListener('pointerdown', (event) => {
    tabbing = false;
    if (host?.hasAttribute('data-tooltip-tap') && host.contains(event.target)) return;

    clicked = hostOf(event);
    hideBubble();
}, true);

// A focus shows the bubble only when Tab moved it, never a focus the app set; a tap host shows it on any focus.
document.addEventListener('focusin', (event) => {
    const target = event.target;
    if (target instanceof Element && target.matches(hosts) && (tabbing || target.hasAttribute('data-tooltip-tap'))) {
        showBubble(target, true);
    }
});

document.addEventListener('focusout', (event) => {
    if (event.target === host) hideBubble();
});

// While a bubble shows, Escape only hides it and reaches neither the dialog nor the app.
window.addEventListener('keydown', (event) => {
    tabbing = event.key === 'Tab';
    if (event.key !== 'Escape' || !host) return;

    event.preventDefault();
    event.stopPropagation();
    hideBubble();
}, true);
