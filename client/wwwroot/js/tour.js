// Locates the element marked with a given data-tour id so TourGuide.razor can position its
// spotlight/callout around it — returns null (rather than throwing) if the target isn't on
// the page right now, which the caller treats as "nothing to highlight yet".
window.tour = {
    getRect: function (tourId) {
        const el = document.querySelector('[data-tour="' + tourId + '"]');
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return { top: r.top, left: r.left, width: r.width, height: r.height };
    },

    // Generic version of getRect for an arbitrary CSS selector (used by TourGuide to measure
    // its own callout box after render, so it can clamp it back on-screen).
    getElementRect: function (selector) {
        const el = document.querySelector(selector);
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return { top: r.top, left: r.left, width: r.width, height: r.height };
    },

    getViewport: function () {
        return { width: window.innerWidth, height: window.innerHeight };
    },
};

// Keeps Axon's toggle button (and its panel, stacked directly above it) from ever landing on
// top of another fixed-position control — several pages anchor their own floating buttons
// (Ask AI toggle, the reader's "Back" link) to the same bottom-right corner Axon lives in.
// Every such element is marked with data-floating-widget; whenever the DOM changes (a
// MutationObserver, since Axon is mounted once in MainLayout and never re-inits on
// navigation) this nudges Axon's `bottom` offset above whichever one it would otherwise
// overlap, and resets back to its default corner spot once nothing conflicts anymore.
(function () {
    var GAP = 10;
    var DEFAULT_TOGGLE_BOTTOM = 28; // 1.75rem
    var PANEL_GAP = 8; // matches the default 5.5rem panel bottom - 1.75rem toggle bottom - toggle height

    function reposition() {
        var toggle = document.querySelector('.axon-toggle');
        if (!toggle) return;

        toggle.style.bottom = '';
        var toggleRect = toggle.getBoundingClientRect();

        var pushBottom = 0;
        document.querySelectorAll('[data-floating-widget]').forEach(function (el) {
            var r = el.getBoundingClientRect();
            if (r.width === 0 && r.height === 0) return;

            var overlaps = toggleRect.left < r.right && toggleRect.right > r.left &&
                toggleRect.top < r.bottom && toggleRect.bottom > r.top;
            if (!overlaps) return;

            var neededBottom = (window.innerHeight - r.top) + GAP;
            if (neededBottom > pushBottom) pushBottom = neededBottom;
        });

        var appliedToggleBottom = DEFAULT_TOGGLE_BOTTOM;
        if (pushBottom > 0) {
            toggle.style.bottom = pushBottom + 'px';
            appliedToggleBottom = pushBottom;
        }

        var panel = document.querySelector('.axon-panel');
        if (panel) {
            var toggleHeight = toggleRect.height || 52;
            panel.style.bottom = (appliedToggleBottom + toggleHeight + PANEL_GAP) + 'px';
        }
    }

    var scheduled = false;
    function scheduleReposition() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(function () {
            scheduled = false;
            reposition();
        });
    }

    window.axonLayout = { reposition: scheduleReposition };

    var observer = new MutationObserver(scheduleReposition);
    observer.observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['class', 'style'] });
    window.addEventListener('resize', scheduleReposition);
    scheduleReposition();
})();
