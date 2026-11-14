/* ME Workshop — gallery grid, filters and lightbox (gallery page and home-page strip).
   Items come from window.ME_CONTENT.gallery, in the same order as the tiles. */
(function () {
    'use strict';
    if (window.__meGallery) return; // the script can be included twice on one page
    window.__meGallery = true;

    var items = ((window.ME_CONTENT || {}).gallery) || [];

    function lang() { return (window.ME && window.ME.lang) || document.documentElement.lang || 'ar'; }
    function caption(it) {
        if (!it) return '';
        var l = lang();
        var c = l === 'en' ? it.captionEn : (l === 'ku' ? it.captionKu : it.captionAr);
        return c || it.captionAr || it.captionEn || it.captionKu || '';
    }

    function init() {
        var box = document.getElementById('lightbox');
        if (!box || !items.length) { setupFilters(); return; }
        var stage = document.getElementById('lbStage');
        var cap = document.getElementById('lbCap');
        var count = document.getElementById('lbCount');
        var prev = document.getElementById('lbPrev');
        var next = document.getElementById('lbNext');
        var closeBtn = document.getElementById('lbClose');
        var order = [];      // indexes the viewer steps through (follows the active filter)
        var pos = 0;
        var lastFocus = null;

        function visibleOrder() {
            var list = [];
            document.querySelectorAll('[data-gallery] .g-tile').forEach(function (li) {
                if (li.hidden || getComputedStyle(li).display === 'none') return;
                var b = li.querySelector('.g-open');
                var i = parseInt(b.getAttribute('data-index'), 10);
                if (list.indexOf(i) === -1) list.push(i);
            });
            return list;
        }

        function render() {
            var it = items[order[pos]];
            stage.innerHTML = '';
            var el;
            if (it.type === 'video') {
                el = document.createElement('video');
                el.src = it.url;
                el.controls = true;
                el.playsInline = true;
                el.setAttribute('playsinline', '');
                el.preload = 'metadata';
                el.autoplay = true;
            } else {
                el = document.createElement('img');
                el.src = it.url;
                el.alt = caption(it);
                el.decoding = 'async';
            }
            stage.appendChild(el);
            cap.textContent = caption(it);
            count.textContent = (pos + 1) + ' / ' + order.length;
            prev.disabled = pos === 0;
            next.disabled = pos === order.length - 1;
            // warm the next photo so stepping feels instant
            var n = items[order[pos + 1]];
            if (n && n.type === 'image') { var pre = new Image(); pre.src = n.url; }
        }

        function open(index) {
            order = visibleOrder();
            pos = Math.max(0, order.indexOf(index));
            lastFocus = document.activeElement;
            box.hidden = false;
            document.body.classList.add('lb-open');
            render();
            closeBtn.focus();
        }
        function close() {
            stage.innerHTML = ''; // stops any playing video
            box.hidden = true;
            document.body.classList.remove('lb-open');
            if (lastFocus && lastFocus.focus) lastFocus.focus();
        }
        function step(d) {
            var p = pos + d;
            if (p < 0 || p >= order.length) return;
            pos = p;
            render();
        }

        document.querySelectorAll('[data-gallery] .g-open').forEach(function (b) {
            b.addEventListener('click', function () { open(parseInt(b.getAttribute('data-index'), 10)); });
        });
        // "Previous" is the start-side button; in RTL that is on the right, which the CSS mirrors.
        prev.addEventListener('click', function () { step(-1); });
        next.addEventListener('click', function () { step(1); });
        closeBtn.addEventListener('click', close);
        box.addEventListener('click', function (e) { if (e.target === box || e.target === stage) close(); });

        document.addEventListener('keydown', function (e) {
            if (box.hidden) return;
            var rtl = document.documentElement.dir === 'rtl';
            if (e.key === 'Escape') { e.preventDefault(); close(); }
            else if (e.key === 'ArrowRight') { e.preventDefault(); step(rtl ? -1 : 1); }
            else if (e.key === 'ArrowLeft') { e.preventDefault(); step(rtl ? 1 : -1); }
            else if (e.key === 'Tab') {
                // keep focus inside the viewer
                var f = Array.prototype.filter.call(box.querySelectorAll('button, video'), function (x) { return !x.disabled && x.offsetParent !== null; });
                if (!f.length) return;
                var first = f[0], last = f[f.length - 1];
                if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
                else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
            }
        });

        // Swipe on touch screens: finger moves the way the content should go.
        var x0 = null, y0 = null;
        stage.addEventListener('touchstart', function (e) {
            if (e.touches.length !== 1) { x0 = null; return; }
            x0 = e.touches[0].clientX; y0 = e.touches[0].clientY;
        }, { passive: true });
        stage.addEventListener('touchend', function (e) {
            if (x0 === null) return;
            var dx = e.changedTouches[0].clientX - x0;
            var dy = e.changedTouches[0].clientY - y0;
            x0 = null;
            if (Math.abs(dx) < 50 || Math.abs(dx) < Math.abs(dy) * 1.3) {
                if (dy > 90 && Math.abs(dy) > Math.abs(dx) * 1.3) close(); // swipe down closes
                return;
            }
            var rtl = document.documentElement.dir === 'rtl';
            // LTR: swipe left = next. RTL: swipe right = next.
            step((dx < 0) !== rtl ? 1 : -1);
        }, { passive: true });

        document.addEventListener('me:lang', function () {
            if (!box.hidden) { cap.textContent = caption(items[order[pos]]); }
            paintTileCaptions();
        });

        setupFilters();
        paintTileCaptions();
    }

    function paintTileCaptions() {
        document.querySelectorAll('[data-gallery] .g-cap').forEach(function (el) {
            var i = parseInt(el.getAttribute('data-cap-index'), 10);
            el.textContent = caption(items[i]);
        });
    }

    function setupFilters() {
        var buttons = document.querySelectorAll('.g-filters [data-filter]');
        buttons.forEach(function (b) {
            b.addEventListener('click', function () {
                var f = b.getAttribute('data-filter');
                buttons.forEach(function (x) { x.setAttribute('aria-pressed', x === b ? 'true' : 'false'); });
                document.querySelectorAll('[data-gallery="page"] .g-tile').forEach(function (li) {
                    li.hidden = f !== 'all' && li.getAttribute('data-type') !== f;
                });
            });
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
