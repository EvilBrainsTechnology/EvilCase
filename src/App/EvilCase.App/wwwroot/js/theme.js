// The theme is applied by the inline script in index.html before Blazor boots; this reads it back.
export function isDarkTheme() {
    return document.body.getAttribute("data-bs-theme") === "dark";
}
