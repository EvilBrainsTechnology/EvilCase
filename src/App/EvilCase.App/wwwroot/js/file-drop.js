// Forwards a drop anywhere on the card to the hidden InputFile: InputFile only reacts to a native
// "change" event on its own <input>, so a card-wide drop has to set that input's files and dispatch one.
export function bindCardDrop(cardElement, inputWrapperElement, dotNetRef) {
    const inputElement = inputWrapperElement.querySelector("input[type=file]");

    function prevent(event) {
        event.preventDefault();
    }

    function enter(event) {
        prevent(event);
        dotNetRef.invokeMethodAsync("SetDragOver", true);
    }

    function leave(event) {
        prevent(event);

        // A dragleave bubbles from every child the pointer crosses; only a leave whose
        // relatedTarget is outside the card itself actually leaves the drop area.
        if (event.relatedTarget && cardElement.contains(event.relatedTarget))
            return;

        dotNetRef.invokeMethodAsync("SetDragOver", false);
    }

    function drop(event) {
        prevent(event);
        dotNetRef.invokeMethodAsync("SetDragOver", false);

        if (event.dataTransfer && event.dataTransfer.files.length > 0) {
            inputElement.files = event.dataTransfer.files;
            inputElement.dispatchEvent(new Event("change", { bubbles: true }));
        }
    }

    cardElement.addEventListener("dragenter", enter);
    cardElement.addEventListener("dragover", prevent);
    cardElement.addEventListener("dragleave", leave);
    cardElement.addEventListener("drop", drop);

    return {
        dispose() {
            cardElement.removeEventListener("dragenter", enter);
            cardElement.removeEventListener("dragover", prevent);
            cardElement.removeEventListener("dragleave", leave);
            cardElement.removeEventListener("drop", drop);
        },
    };
}
