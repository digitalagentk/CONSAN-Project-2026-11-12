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
        var code = document.getElementById('langCode');
        if (code) code.textContent = lang === 'en' ? 'EN' : (lang === 'ku' ? 'KU' : 'ع');
        closeLangMenu();
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

    // ---- compact language menu (desktop) ----------------------------------
    function closeLangMenu() {
        var m = document.getElementById('langMenu');
        var b = document.getElementById('langCurrent');
        if (m) m.classList.remove('open');
        if (b) b.setAttribute('aria-expanded', 'false');
    }
    window.toggleLangMenu = function (e) {
        if (e) e.stopPropagation();
        var m = document.getElementById('langMenu');
        var b = document.getElementById('langCurrent');
        if (!m) return;
        var open = !m.classList.contains('open');
        m.classList.toggle('open', open);
        if (b) b.setAttribute('aria-expanded', open ? 'true' : 'false');
        if (open) {
            var cur = m.querySelector('.lang-switch [aria-pressed="true"]');
            if (cur) cur.focus();
        }
    };

    // ---- contact sheet --------------------------------------------------
    // remember=true when the visitor closes the numbers panel themselves: it then stays
    // closed for the rest of this visit; it opens again on their next visit.
    window.toggleContact = function (force, remember) {
        var box = document.getElementById('floatingContact');
        if (!box) return;
        var open = typeof force === 'boolean' ? force : !box.classList.contains('open');
        box.classList.toggle('open', open);
        var re = box.querySelector('.contact-reopen');
        if (re) re.setAttribute('aria-expanded', open ? 'true' : 'false');
        try {
            if (open) sessionStorage.removeItem('me-contact-closed');
            else if (remember) sessionStorage.setItem('me-contact-closed', '1');
        } catch (e) { }
    };
    window.openWhatsApp = function (number) {
        window.open('https://wa.me/' + String(number).replace(/\D/g, ''), '_blank', 'noopener');
    };

    function ready() {
        // Inner pages define their own switchLanguage for their own text. Keep it,
        // and put ours in front so the choice is remembered and the header follows.
        if (typeof window.switchLanguage === 'function') pageSwitch = window.switchLanguage;
        window.switchLanguage = function (lang) { setLang(lang); };

        try { if (sessionStorage.getItem('me-contact-closed')) window.toggleContact(false); } catch (e) { }

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
            var lm = document.getElementById('langMenu');
            if (lm && lm.classList.contains('open') && !lm.contains(e.target)) closeLangMenu();
        });
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { closeMenu(); closeLangMenu(); }
        });

        // Count this page view. Someone who has not answered the cookie question yet is
        // counted without a cookie; the bar then asks once.
        sendVisit();
        consentBar();

        document.querySelectorAll('[data-years]').forEach(function (el) { el.textContent = years; });
        document.querySelectorAll('[data-year-now]').forEach(function (el) { el.textContent = new Date().getFullYear(); });
    }

    // ---- cookies & visit counting ------------------------------------------
    function cookie(name) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : null;
    }
    // only=true just issues the visitor cookie (after "Accept") without counting the page twice
    function sendVisit(only) {
        try {
            var body = JSON.stringify({ path: location.pathname, ref: document.referrer || '', lang: ME.lang, w: String(screen.width || 0), only: only ? '1' : '' });
            fetch('/api/visit', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: body, keepalive: true, credentials: 'same-origin' }).catch(function () { });
        } catch (e) { }
    }
    function setConsent(value) {
        var secure = location.protocol === 'https:' ? '; Secure' : '';
        document.cookie = 'me_consent=' + value + '; Max-Age=31536000; Path=/; SameSite=Lax' + secure;
    }
    function consentBar() {
        if (cookie('me_consent')) return;
        var bar = document.createElement('div');
        bar.className = 'consent-bar';
        bar.setAttribute('role', 'region');
        bar.setAttribute('aria-label', 'Cookies');
        bar.innerHTML = '<p data-i18n="cookieText"></p><div class="consent-actions">' +
            '<button type="button" class="btn btn-brass" data-c="yes" data-i18n="cookieAccept"></button>' +
            '<button type="button" class="btn btn-line" data-c="no" data-i18n="cookieDecline"></button></div>';
        document.body.appendChild(bar);
        ME.paint(bar);
        var fit = function () { root.style.setProperty('--consent-h', bar.offsetHeight + 'px'); };
        document.body.classList.add('consent-open');
        fit();
        window.addEventListener('resize', fit);
        bar.addEventListener('click', function (e) {
            var b = e.target.closest('[data-c]');
            if (!b) return;
            var yes = b.getAttribute('data-c') === 'yes';
            setConsent(yes ? 'yes' : 'no');
            bar.remove();
            document.body.classList.remove('consent-open');
            window.removeEventListener('resize', fit);
            if (yes) sendVisit(true); // issue the visitor cookie now
        });
        document.addEventListener('me:lang', function () { if (bar.isConnected) { ME.paint(bar); fit(); } });
    }

    // Deferred scripts run before DOMContentLoaded; waiting for it lets the page's own script go first.
    if (document.readyState !== 'complete') document.addEventListener('DOMContentLoaded', ready);
    else ready();
})();
