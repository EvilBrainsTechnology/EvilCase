// index.html loads this module so the session refresh runs while Blazor downloads; Blazor imports
// the same module instance. The body carries no token: both stay in HttpOnly cookies.

// The timeout bounds how long a stalled request holds the lock every tab waits on.
async function post() {
    const response = await fetch(new URL('api/auth/refresh', document.baseURI), { method: 'POST', credentials: 'include', signal: AbortSignal.timeout(30000) });
    return { status: response.status, body: response.status === 200 ? await response.text() : null };
}

// Under the lock a 409 comes only from a tab without it; its replacement cookie lands moments later.
async function postRetryingSuperseded() {
    const result = await post();

    if (result.status !== 409)
        return result;

    await new Promise(resolve => setTimeout(resolve, 250));
    return post();
}

// The lock spans every tab of the origin, so each refresh sends the cookie the one before it left.
// Null when the request did not complete.
export function refreshSession() {
    const refresh = navigator.locks ? navigator.locks.request('evilcase-refresh', postRetryingSuperseded) : postRetryingSuperseded();
    return refresh.catch(() => null);
}

let pending = refreshSession();

// Hands the result over once.
export function takeEarlyRefresh() {
    const result = pending;
    pending = Promise.resolve(null);
    return result;
}
