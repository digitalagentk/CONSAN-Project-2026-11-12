/* ME Workshop — Middle Euphrates Workshop
   Shared behaviour for every public page: language, theme, header, contact sheet. */
(function () {
    'use strict';

    var root = document.documentElement;
    var C = window.ME_CONTENT || { t: { ar: {}, en: {}, ku: {} }, founded: 1945, phones: [] };
    var LANGS = ['ar', 'en', 'ku'];
    var years = new Date().getFullYear() - (C.founded || 1945);

    function store(key, value) {
        try {
            if (value === undefined) return localStorage.getItem(key);
            localStorage.setItem(key, value);
        } catch (e) { /* private mode: carry on without remembering */ }
        return null;
    }

    var ME = window.ME = {
        lang: 'ar',
        years: years,
        text: function (key, lang) {
            var d = C.t[lang || ME.lang] || {};
            var v = d[key];
            if (v === undefined || v === '') v = (C.t.ar || {})[key];
            return v === undefined ? '' : String(v).replace('{n}', years);
        },
        // Writes the current language into everything marked data-i18n.
        paint: function (scope) {
            (scope || document).querySelectorAll('[data-i18n]').forEach(function (el) {
                var v = ME.text(el.getAttribute('data-i18n'));
                if (v !== '') el.innerHTML = v;
            });
            (scope || document).querySelectorAll('[data-i18n-label]').forEach(function (el) {
                var v = ME.text(el.getAttribute('data-i18n-label'));
                if (v !== '') el.setAttribute('aria-label', v);
            });
        }
    };

    // ---- language -------------------------------------------------------
    var pageSwitch = null; // an inner page's own language function, when it has one

    function setLang(lang, remember) {
        if (LANGS.indexOf(lang) === -1) lang = 'ar';
        ME.lang = lang;
        root.lang = lang;
        root.dir = lang === 'en' ? 'ltr' : 'rtl';
        if (remember !== false) store('me-lang', lang);
        if (pageSwitch) {
            try { pageSwitch(lang); } catch (e) { if (window.console) console.warn(e); }
        }
        ME.paint();
        document.querySelectorAll('.lang-switch button').forEach(function (b) {
            b.setAttribute('aria-pressed', b.getAttribute('data-lang') === lang ? 'true' : 'false');
        });
        root.classList.remove('me-pending');
        document.dispatchEvent(new CustomEvent('me:lang', { detail: lang }));
    }

    // ---- theme ----------------------------------------------------------
    window.toggleTheme = function () {
        var next = root.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
        root.setAttribute('data-theme', next);
        store('me-theme', next);
        document.dispatchEvent(new CustomEvent('me:theme', { detail: next }));
    };

    // ---- header ---------------------------------------------------------
    function closeMenu() {
        var menu = document.getElementById('navMenu');
        var toggle = document.querySelector('.nav-toggle');
        var nav = document.getElementById('navbar');
        if (menu) menu.classList.remove('active');
        if (nav) nav.classList.remove('menu-open');
        if (toggle) { toggle.classList.remove('active'); toggle.setAttribute('aria-expanded', 'false'); }
    }
    window.toggleMenu = function () {
        var menu = document.getElementById('navMenu');
        var toggle = document.querySelector('.nav-toggle');
        var nav = document.getElementById('navbar');
        if (!menu) return;
        var open = !menu.classList.contains('active');
        menu.classList.toggle('active', open);
        if (nav) nav.classList.toggle('menu-open', open);
        if (toggle) { toggle.classList.toggle('active', open); toggle.setAttribute('aria-expanded', open ? 'true' : 'false'); }
    };
    window.toggleLangDropdown = function () { };

    // ---- contact sheet --------------------------------------------------
    window.toggleContact = function (force) {
        var box = document.getElementById('floatingContact');
        if (!box) return;
        var open = typeof force === 'boolean' ? force : !box.classList.contains('open');
        box.classList.toggle('open', open);
        var badge = box.querySelector('.contact-badge');
        if (badge) badge.setAttribute('aria-expanded', open ? 'true' : 'false');
    };
    window.openWhatsApp = function (number) {
        window.open('https://wa.me/' + String(number).replace(/\D/g, ''), '_blank', 'noopener');
    };

    function ready() {
        // Inner pages define their own switchLanguage for their own text. Keep it,
        // and put ours in front so the choice is remembered and the header follows.
        if (typeof window.switchLanguage === 'function') pageSwitch = window.switchLanguage;
        window.switchLanguage = function (lang) { setLang(lang); };

        var saved = store('me-lang');
        if (saved === 'en' || saved === 'ku') setLang(saved, false);
        else root.classList.remove('me-pending');

        var nav = document.getElementById('navbar');
        if (nav) {
            var onScroll = function () {
                nav.classList.toggle('scrolled', window.scrollY > 40);
                document.body.classList.toggle('past-hero', window.scrollY > window.innerHeight * 0.7);
            };
            window.addEventListener('scroll', onScroll, { passive: true });
            onScroll();
        }

        // Mark the current page in the menu.
        var path = location.pathname.replace(/\/+$/, '') || '/';
        document.querySelectorAll('.nav-menu .nav-link').forEach(function (a) {
            var href = a.getAttribute('href');
            if (href === path && path !== '/') a.setAttribute('aria-current', 'page');
        });

        document.querySelectorAll('.nav-menu a').forEach(function (a) { a.addEventListener('click', closeMenu); });

        document.addEventListener('click', function (e) {
            var menu = document.getElementById('navMenu');
            var toggle = document.querySelector('.nav-toggle');
            if (menu && menu.classList.contains('active') && !menu.contains(e.target) && toggle && !toggle.contains(e.target)) closeMenu();
            var box = document.getElementById('floatingContact');
            if (box && box.classList.contains('open') && !box.contains(e.target)) window.toggleContact(false);
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { closeMenu(); window.toggleContact(false); }
        });

        document.querySelectorAll('[data-years]').forEach(function (el) { el.textContent = years; });
        document.querySelectorAll('[data-year-now]').forEach(function (el) { el.textContent = new Date().getFullYear(); });
    }

    // Deferred scripts run before DOMContentLoaded; waiting for it lets the page's own script go first.
    if (document.readyState !== 'complete') document.addEventListener('DOMContentLoaded', ready);
    else ready();
})();
