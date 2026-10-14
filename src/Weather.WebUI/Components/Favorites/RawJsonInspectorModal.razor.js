// Focus trap + focus restore for the raw JSON inspector modal.
const restore = new WeakMap();

function onKeyDown(event) {
    if (event.key !== 'Tab') {
        return;
    }

    const focusables = event.currentTarget.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
    if (focusables.length === 0) {
        return;
    }

    const first = focusables[0];
    const last = focusables[focusables.length - 1];

    if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
    }
}

export function trapFocus(element) {
    if (!element) {
        return;
    }

    restore.set(element, document.activeElement);
    element.addEventListener('keydown', onKeyDown);
    element.focus();
}

export function releaseFocus(element) {
    if (!element) {
        return;
    }

    element.removeEventListener('keydown', onKeyDown);
    const previous = restore.get(element);
    if (previous && typeof previous.focus === 'function') {
        previous.focus();
    }
    restore.delete(element);
}
