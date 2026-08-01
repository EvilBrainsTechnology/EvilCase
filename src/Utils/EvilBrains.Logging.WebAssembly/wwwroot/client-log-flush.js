let handler = null;
let listener = null;

// A fetch started while the page is going away is aborted with the document, so the payload leaves
// through sendBeacon, which the browser keeps alive after the page is gone. The event is pagehide
// rather than beforeunload: mobile Safari does not fire beforeunload, and a beforeunload listener
// keeps the page out of the back/forward cache.
export function register(url, flushHandler) {
    handler = flushHandler;
    listener = () => {
        // Flush hands over one batch at a time, so it is drained until empty. A beacon refused over
        // quota ends the loop: the browser would refuse the rest of the queue too.
        let payload;
        while ((payload = handler.invokeMethod('Flush')) !== null) {
            if (!navigator.sendBeacon(url, new Blob([payload], { type: 'application/json' })))
                break;
        }
    };

    addEventListener('pagehide', listener);
}

export function unregister() {
    removeEventListener('pagehide', listener);

    handler = null;
    listener = null;
}
