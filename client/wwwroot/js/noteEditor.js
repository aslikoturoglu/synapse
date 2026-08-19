window.noteEditor = {
    exec: function (command, value) {
        document.execCommand(command, false, value || null);
    },
    getHtml: function (element) {
        return element ? element.innerHTML : "";
    },
    // Returns the current selection's text, but only if it's actually inside this
    // page's element — otherwise the toolbar's "Ask AI" click would pick up a selection
    // left over in the other (left/right) page.
    getSelectionText: function (element) {
        const sel = window.getSelection();
        if (!element || !sel || sel.rangeCount === 0 || sel.isCollapsed) return "";
        const range = sel.getRangeAt(0);
        if (!element.contains(range.commonAncestorContainer)) return "";
        return sel.toString();
    },
    // Wraps the current selection in a <mark data-highlight-id> so it stays visible once
    // the page's HTML is persisted back into NotePage.Body. Only works for selections that
    // stay inside a single text node (surroundContents' limitation) — fine for highlighting
    // a phrase, not a selection spanning multiple paragraphs.
    wrapSelectionAsHighlight: function (element, highlightId) {
        const sel = window.getSelection();
        if (!element || !sel || sel.rangeCount === 0 || sel.isCollapsed) return element ? element.innerHTML : "";
        const range = sel.getRangeAt(0);
        if (!element.contains(range.commonAncestorContainer)) return element.innerHTML;

        const mark = document.createElement("mark");
        mark.className = "note-highlight";
        mark.dataset.highlightId = highlightId;
        try {
            range.surroundContents(mark);
        } catch {
            return element.innerHTML;
        }
        sel.removeAllRanges();
        return element.innerHTML;
    },
    scrollToHighlight: function (element, highlightId) {
        const el = element && element.querySelector('[data-highlight-id="' + highlightId + '"]');
        if (el) el.scrollIntoView({ behavior: "smooth", block: "center" });
    },
    // Dismissing a bubble: unwraps the <mark data-highlight-id> back into plain content,
    // keeping the text itself (including anything Add to Document already inserted) but
    // dropping the highlight styling/marker for good, so it doesn't linger in the persisted
    // page HTML forever.
    removeHighlight: function (element, highlightId) {
        if (!element) return "";
        const mark = element.querySelector('[data-highlight-id="' + highlightId + '"]');
        if (mark) {
            const parent = mark.parentNode;
            while (mark.firstChild) parent.insertBefore(mark.firstChild, mark);
            parent.removeChild(mark);
        }
        return element.innerHTML;
    },
    // "Add to Document": inserts the given html right after the highlighted mark, or
    // (replace=true) swaps the mark out for it entirely. If the mark isn't found (e.g. the
    // page was re-rendered since), the document is left untouched.
    applyDocumentEdit: function (element, highlightId, html, replace) {
        if (!element) return "";
        const mark = element.querySelector('[data-highlight-id="' + highlightId + '"]');
        if (!mark) return element.innerHTML;

        const inserted = document.createElement("span");
        inserted.className = "note-ai-inserted";
        inserted.innerHTML = " " + html;

        if (replace) {
            mark.replaceWith(inserted);
        } else {
            mark.after(inserted);
        }
        return element.innerHTML;
    },
    // Ask AI mode: selecting a word/phrase (drag-select, or double-click which selects the
    // word under the cursor) shows a small floating "Ask to AI" button right next to the
    // selection — the highlight itself is only created once the user actually clicks it, so
    // highlighting always stays a deliberate user action, never automatic.
    //
    // NotePageView keys its <p> on Page.Body, so applying a highlight destroys this exact DOM
    // node and Blazor mounts a brand new one — this function gets called again for the new
    // node (NotePageView calls it on every render, not just the first), and the
    // dataset.selectionListenerAttached guard just stops us from double-attaching if it's
    // called again for a node that already has a listener.
    onSelectionMade: function (element, dotNetRef, methodName) {
        if (!element || element.dataset.selectionListenerAttached) return;
        element.dataset.selectionListenerAttached = "1";

        let floatBtn = null;
        const removeFloatBtn = () => {
            if (floatBtn) {
                floatBtn.remove();
                floatBtn = null;
            }
        };

        element.addEventListener("mouseup", () => {
            removeFloatBtn();

            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0 || sel.isCollapsed) return;
            const range = sel.getRangeAt(0);
            if (!element.contains(range.commonAncestorContainer)) return;
            if (!sel.toString().trim()) return;

            const rect = range.getBoundingClientRect();
            floatBtn = document.createElement("button");
            floatBtn.type = "button";
            floatBtn.className = "note-ai-float-btn";
            floatBtn.textContent = "Ask to AI";
            floatBtn.style.left = (rect.left + rect.width / 2) + "px";
            floatBtn.style.top = rect.top + "px";
            // Keep the browser selection alive through the click — a plain click on any
            // element outside the selection collapses it before the click handler runs.
            floatBtn.addEventListener("mousedown", e => e.preventDefault());
            floatBtn.addEventListener("click", () => {
                dotNetRef.invokeMethodAsync(methodName || "NotifyTextSelected");
                removeFloatBtn();
            });
            document.body.appendChild(floatBtn);
        });

        document.addEventListener("mousedown", e => {
            if (floatBtn && e.target !== floatBtn) removeFloatBtn();
        });
    },
};
