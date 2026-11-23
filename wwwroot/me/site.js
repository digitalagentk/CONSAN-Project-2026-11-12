/* ME Workshop — Middle Euphrates Workshop
   Shared behaviour for every public page: language, theme, header, contact sheet. */
(function () {
    'use strict';

    var root = document.documentElement;
    var C = window.ME_CONTENT || { t: { ar: {}, en: {}, ku: {}, fa: {}, tr: {} }, founded: 1945, phones: [] };
    var LANGS = ['ar', 'ku', 'fa', 'tr', 'en'];   // the order visitors see them in
    var RTL = { ar: 1, ku: 1, fa: 1 };
    var CODES = { ar: 'ع', en: 'EN', ku: 'KU', fa: 'FA', tr: 'TR' };
    // Where a text has not been written in a language yet, show the closest one instead.
    var FALLBACK = { ar: ['ar', 'en'], en: ['en', 'ar'], ku: ['ku', 'ar', 'en'], fa: ['fa', 'ar', 'en'], tr: ['tr', 'en', 'ar'] };
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
        langs: LANGS,
        years: years,
        isRtl: function (lang) { return !!RTL[lang || ME.lang]; },
        // item.captionAr / captionEn / captionKu / captionFa / captionTr -> the one for the current language
        pick: function (item, base, lang) {
            if (!item) return '';
            var order = FALLBACK[lang || ME.lang] || FALLBACK.ar;
            for (var i = 0; i < order.length; i++) {
                var v = item[base + order[i].charAt(0).toUpperCase() + order[i].slice(1)];
                if (v) return v;
            }
            return '';
        },
        text: function (key, lang) {
            var d = C.t[lang || ME.lang] || {};
            var v = d[key];
            var order = FALLBACK[lang || ME.lang] || FALLBACK.ar;
            for (var i = 1; (v === undefined || v === '') && i < order.length; i++) v = (C.t[order[i]] || {})[key];
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
        root.dir = RTL[lang] ? 'rtl' : 'ltr';
        if (remember !== false) store('me-lang', lang);
        if (pageSwitch) {
            try { pageSwitch(lang); } catch (e) { if (window.console) console.warn(e); }
        }
        ME.paint();
        document.querySelectorAll('.lang-switch button').forEach(function (b) {
            b.setAttribute('aria-pressed', b.getAttribute('data-lang') === lang ? 'true' : 'false');
        });
        var code = document.getElementById('langCode');
        if (code) code.textContent = CODES[lang] || 'ع';
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
        var known = saved && LANGS.indexOf(saved) !== -1;
        if (known && saved !== 'ar') setLang(saved, false);
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
        // First visit: ask for the language before anything else; the cookie question waits.
        if (known) consentBar(); else languageGate();

        document.querySelectorAll('[data-years]').forEach(function (el) { el.textContent = years; });
        document.querySelectorAll('[data-year-now]').forEach(function (el) { el.textContent = new Date().getFullYear(); });
    }


    // ---- first visit: choose a language -----------------------------------
    var GATE = [
        ['ar', 'العربية', 'متابعة بالعربية'],
        ['ku', 'کوردی', 'بەردەوام بوون بە کوردی'],
        ['fa', 'فارسی', 'ادامه به فارسی'],
        ['tr', 'Türkçe', 'Türkçe devam et'],
        ['en', 'English', 'Continue in English']
    ];
    function languageGate() {
        var brand = document.querySelector('.brand-link .brand-icon');
        var box = document.createElement('div');
        box.className = 'lang-gate';
        box.setAttribute('role', 'dialog');
        box.setAttribute('aria-modal', 'true');
        box.setAttribute('aria-labelledby', 'langGateTitle');
        box.innerHTML =
            '<div class="lang-gate-card">' +
                '<div class="lang-gate-brand">' + (brand ? brand.outerHTML : '') +
                    '<span><b>ME WORKSHOP</b><small>ورشة الفرات الأوسط · Middle Euphrates Workshop</small></span></div>' +
                '<h2 id="langGateTitle"><span lang="ar" dir="rtl">اختر لغتك</span><small lang="en" dir="ltr">Choose your language</small></h2>' +
                '<div class="lang-gate-list">' + GATE.map(function (g) {
                    var rtl = RTL[g[0]];
                    return '<button type="button" data-lang="' + g[0] + '" lang="' + g[0] + '" dir="' + (rtl ? 'rtl' : 'ltr') + '"><b>' + g[1] + '</b><small>' + g[2] + '</small>' +
                        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="' + (rtl ? 'M15 6l-6 6 6 6' : 'M9 6l6 6-6 6') + '"/></svg></button>';
                }).join('') + '</div>' +
            '</div>';
        document.body.appendChild(box);
        document.body.classList.add('lang-gate-open');
        var first = box.querySelector('button');
        setTimeout(function () { if (first) first.focus(); }, 50);
        box.addEventListener('keydown', function (e) {
            if (e.key !== 'Tab') return;
            var bs = box.querySelectorAll('button'), a = bs[0], z = bs[bs.length - 1];
            if (e.shiftKey && document.activeElement === a) { e.preventDefault(); z.focus(); }
            else if (!e.shiftKey && document.activeElement === z) { e.preventDefault(); a.focus(); }
        });
        box.addEventListener('click', function (e) {
            var b = e.target.closest('button[data-lang]');
            if (!b) return;
            var lang = b.getAttribute('data-lang');
            setLang(lang, true);
            box.classList.add('closing');
            setTimeout(function () { box.remove(); }, 220);
            document.body.classList.remove('lang-gate-open');
            ME.chosen = true;
            consentBar();
            document.dispatchEvent(new CustomEvent('me:lang-chosen', { detail: lang }));
        });
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
