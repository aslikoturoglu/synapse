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
    // Ask AI mode: once the user is already looking at the single-page view, selecting more
    // text shouldn't require a button click again — it should just make a new bubble appear,
    // the same way it would in a real annotation tool. One listener per element instance
    // (the element itself is thrown away and replaced whenever a highlight is applied, since
    // NotePageView keys the <p> on Page.Body, so there's nothing to unregister here).
    onSelectionMade: function (element, dotNetRef) {
        if (!element) return;
        element.addEventListener("mouseup", () => {
            const sel = window.getSelection();
            if (sel && !sel.isCollapsed && element.contains(sel.anchorNode) && sel.toString().trim().length > 0) {
                dotNetRef.invokeMethodAsync("NotifyTextSelected");
            }
        });
    },
};
