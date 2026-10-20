// index.html loads this module so the session refresh runs while Blazor downloads; Blazor imports
// the same module instance. The body carries no token: both stay in HttpOnly cookies.
let pending = fetch(new URL('api/auth/refresh', document.baseURI), { method: 'POST', credentials: 'include' })
    .then(async response => ({ status: response.status, body: response.status === 200 ? await response.text() : null }))
    .catch(() => null);

// Hands the result over once.
export function takeEarlyRefresh() {
    const result = pending;
    pending = Promise.resolve(null);
    return result;
}
