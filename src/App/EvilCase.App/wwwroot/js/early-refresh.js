// index.html loads this module so the session refresh runs while Blazor downloads; Blazor imports
// the same module instance. The body carries no token: both stay in HttpOnly cookies.

// A 409 means another tab spent the cookie moments ago and the jar is about to hold its replacement.
const supersededRetryDelays = [250, 500, 1000];

async function refresh() {
    for (let attempt = 0; ; attempt++) {
        const response = await fetch(new URL('api/auth/refresh', document.baseURI), { method: 'POST', credentials: 'include' });

        if (response.status !== 409 || attempt === supersededRetryDelays.length)
            return { status: response.status, body: response.status === 200 ? await response.text() : null };

        await new Promise(resolve => setTimeout(resolve, supersededRetryDelays[attempt]));
    }
}

let pending = refresh().catch(() => null);

// Hands the result over once.
export function takeEarlyRefresh() {
    const result = pending;
    pending = Promise.resolve(null);
    return result;
}
