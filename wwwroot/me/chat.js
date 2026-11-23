/* ME Workshop — Middle Euphrates Workshop
   Orion, the AI assistant: a greeting when a visitor arrives, a floating button, and a chat
   panel on every public page. Orion answers questions and turns interest into a booking
   request, a call or a WhatsApp message. The browser only talks to /api/chat on this site;
   the Gemini key stays on the server. */
(function () {
    'use strict';

    var root = document.documentElement;
    var MAX_SHOWN = 30;      // messages kept on screen (and in this tab's storage)
    var HISTORY = 8;         // earlier messages sent with each question
    var STORE_KEY = 'me-orion-v1';
    var GREETED_KEY = 'me-orion-greeted';

    var T = {
        ar: {
            name: 'أوريون', open: 'تحدّث مع أوريون', sub: 'المساعد الذكي لورشة الفرات الأوسط',
            hello: 'أهلاً وسهلاً! أنا أوريون، المساعد الذكي لورشة الفرات الأوسط. كيف أقدر أساعدك اليوم؟ أستطيع أن أحجز لك زيارة للورشة، أو أوصلك بأهل الورشة بالهاتف أو الواتساب، أو أجيب عن أسئلتك.',
            teaser: 'أهلاً بك في ورشة الفرات الأوسط. أنا أوريون، مساعدك الذكي. كيف أقدر أساعدك؟',
            book: 'احجز زيارة', chat: 'اسأل أوريون', bookMsg: 'أريد حجز موعد لزيارة الورشة',
            ph: 'اكتب رسالتك…', send: 'إرسال', close: 'إغلاق المحادثة', clear: 'محادثة جديدة', typing: 'أوريون يكتب',
            call: 'اتصل بالورشة', wa: 'واتساب', dismiss: 'إغلاق',
            note: 'أوريون مساعد آلي يجيب من معلومات الورشة فقط، وقد يخطئ. الأسعار والمواعيد يؤكدها أهل الورشة.',
            err: 'تعذّر الاتصال. تحقق من الإنترنت وحاول مرة أخرى.',
            saved: 'تم حفظ طلب الحجز', savedNote: 'ستتصل بك الورشة لتأكيد الموعد. للسرعة أرسل الطلب على واتساب.',
            sendWa: 'أرسل الطلب على واتساب', when: 'الوقت المفضل',
            chips: ['احجز زيارة للورشة', 'سيارتي فيها مشكلة', 'أين تقع الورشة؟', 'كم سعر التصليح؟']
        },
        ku: {
            name: 'ئۆریۆن', open: 'لەگەڵ ئۆریۆن قسە بکە', sub: 'یاریدەدەری زیرەکی کارگەی فوراتی ناوەڕاست',
            hello: 'بەخێربێیت! من ئۆریۆنم، یاریدەدەری زیرەکی کارگەی فوراتی ناوەڕاست. ئەمڕۆ چۆن یارمەتیت بدەم؟ دەتوانم سەردانێکی کارگەت بۆ تۆمار بکەم، یان بە تەلەفۆن یان واتساپ بە کارگەوە بتبەستمەوە، یان وەڵامی پرسیارەکانت بدەمەوە.',
            teaser: 'بەخێربێیت بۆ کارگەی فوراتی ناوەڕاست. من ئۆریۆنم، یاریدەدەرە زیرەکەکەت. چۆن یارمەتیت بدەم؟',
            book: 'سەردان تۆمار بکە', chat: 'پرسیار لە ئۆریۆن بکە', bookMsg: 'دەمەوێت کاتێک بۆ سەردانی کارگە تۆمار بکەم',
            ph: 'نامەکەت بنووسە…', send: 'ناردن', close: 'داخستنی گفتوگۆ', clear: 'گفتوگۆی نوێ', typing: 'ئۆریۆن دەنووسێت',
            call: 'پەیوەندی بە کارگەوە', wa: 'واتساپ', dismiss: 'داخستن',
            note: 'ئۆریۆن یاریدەدەرێکی خۆکارە و تەنها لە زانیارییەکانی کارگە وەڵام دەداتەوە، لەوانەیە هەڵە بکات. نرخ و کات کارگە خۆی پشتڕاستی دەکاتەوە.',
            err: 'پەیوەندی نەکرا. ئینتەرنێتەکەت بپشکنە و دووبارە هەوڵ بدەرەوە.',
            saved: 'داواکاریی سەردان پاشەکەوت کرا', savedNote: 'کارگە پەیوەندیت پێوە دەکات بۆ پشتڕاستکردنەوەی کات. بۆ خێرایی لە واتساپ بینێرە.',
            sendWa: 'لە واتساپ بینێرە', when: 'کاتی دڵخواز',
            chips: ['سەردانێک تۆمار بکە', 'ئۆتۆمبێلەکەم کێشەی هەیە', 'کارگە لە کوێیە؟', 'نرخی چاککردنەوە چەندە؟']
        },
        fa: {
            name: 'اوریون', open: 'گفتگو با اوریون', sub: 'دستیار هوشمند کارگاه فرات میانه',
            hello: 'خوش آمدید! من اوریون هستم، دستیار هوشمند کارگاه فرات میانه. امروز چطور می‌توانم کمکتان کنم؟ می‌توانم برایتان وقت مراجعه به کارگاه ثبت کنم، شما را تلفنی یا در واتس‌اپ به کارگاه وصل کنم، یا به پرسش‌هایتان پاسخ دهم.',
            teaser: 'به کارگاه فرات میانه خوش آمدید. من اوریون، دستیار هوشمند شما هستم. چطور کمکتان کنم؟',
            book: 'ثبت وقت مراجعه', chat: 'از اوریون بپرسید', bookMsg: 'می‌خواهم برای مراجعه به کارگاه وقت بگیرم',
            ph: 'پیام خود را بنویسید…', send: 'ارسال', close: 'بستن گفتگو', clear: 'گفتگوی تازه', typing: 'اوریون در حال نوشتن',
            call: 'تماس با کارگاه', wa: 'واتس‌اپ', dismiss: 'بستن',
            note: 'اوریون دستیاری خودکار است که فقط از اطلاعات کارگاه پاسخ می‌دهد و ممکن است اشتباه کند. قیمت و زمان را خود کارگاه تأیید می‌کند.',
            err: 'اتصال برقرار نشد. اینترنت خود را بررسی کنید و دوباره تلاش کنید.',
            saved: 'درخواست وقت ثبت شد', savedNote: 'کارگاه برای تأیید زمان با شما تماس می‌گیرد. برای سرعت بیشتر، درخواست را در واتس‌اپ بفرستید.',
            sendWa: 'ارسال در واتس‌اپ', when: 'زمان دلخواه',
            chips: ['ثبت وقت مراجعه', 'خودروی من مشکل دارد', 'کارگاه کجاست؟', 'هزینه تعمیر چقدر است؟']
        },
        tr: {
            name: 'Orion', open: 'Orion ile konuşun', sub: 'Orta Fırat Atölyesi yapay zekâ asistanı',
            hello: 'Hoş geldiniz! Ben Orion, Orta Fırat Atölyesi\'nin yapay zekâ asistanıyım. Bugün size nasıl yardımcı olabilirim? Atölye ziyareti için randevu talebinizi alabilir, sizi telefonla veya WhatsApp\'tan atölyeye bağlayabilir ya da sorularınızı yanıtlayabilirim.',
            teaser: 'Orta Fırat Atölyesi\'ne hoş geldiniz. Ben Orion, yapay zekâ asistanınız. Size nasıl yardımcı olabilirim?',
            book: 'Randevu al', chat: 'Orion\'a sorun', bookMsg: 'Atölyeye gelmek için randevu almak istiyorum',
            ph: 'Mesajınızı yazın…', send: 'Gönder', close: 'Sohbeti kapat', clear: 'Yeni sohbet', typing: 'Orion yazıyor',
            call: 'Atölyeyi ara', wa: 'WhatsApp', dismiss: 'Kapat',
            note: 'Orion, yalnızca atölyenin bilgilerinden yanıt veren otomatik bir asistandır ve hata yapabilir. Fiyat ve saati atölye onaylar.',
            err: 'Bağlantı kurulamadı. İnternetinizi kontrol edip tekrar deneyin.',
            saved: 'Randevu talebiniz kaydedildi', savedNote: 'Atölye saati onaylamak için sizi arayacak. Daha hızlı olması için WhatsApp\'tan gönderin.',
            sendWa: 'WhatsApp\'tan gönder', when: 'Tercih edilen zaman',
            chips: ['Randevu almak istiyorum', 'Aracımda sorun var', 'Atölye nerede?', 'Onarım ne kadar tutar?']
        },
        en: {
            name: 'Orion', open: 'Chat with Orion', sub: 'Middle Euphrates Workshop AI assistant',
            hello: "Welcome! I'm Orion, the AI assistant of Middle Euphrates Workshop. How can I help you today? I can book a visit to the workshop for you, connect you with the workshop by phone or WhatsApp, or answer your questions.",
            teaser: "Welcome to Middle Euphrates Workshop. I'm Orion, your AI assistant. How can I help?",
            book: 'Book a visit', chat: 'Ask Orion', bookMsg: "I'd like to book a visit to the workshop",
            ph: 'Type your message…', send: 'Send', close: 'Close chat', clear: 'New chat', typing: 'Orion is typing',
            call: 'Call the workshop', wa: 'WhatsApp', dismiss: 'Close',
            note: 'Orion is an automated assistant that answers only from the workshop’s information and can make mistakes. The workshop confirms prices and times.',
            err: 'Could not connect. Check your internet and try again.',
            saved: 'Booking request saved', savedNote: 'The workshop will call you to confirm the time. For a faster reply, send it on WhatsApp.',
            sendWa: 'Send on WhatsApp', when: 'Preferred time',
            chips: ['Book a visit', 'My vehicle has a problem', 'Where is the workshop?', 'How much does a repair cost?']
        }
    };

    function lang() { var l = (window.ME && window.ME.lang) || root.lang || 'ar'; return T[l] ? l : 'ar'; }
    function t(k) { return T[lang()][k]; }

    var C = window.ME_CONTENT || {};
    var MAIN = (C.phones && C.phones[0] && C.phones[0].intl) || '9647809744298';
    var TEL = 'tel:+' + MAIN;
    var WA = 'https://wa.me/' + MAIN;

    function read() {
        try { var v = JSON.parse(sessionStorage.getItem(STORE_KEY) || 'null'); if (v && v.sid && Array.isArray(v.msgs)) return v; } catch (e) { }
        return null;
    }
    function save() { try { sessionStorage.setItem(STORE_KEY, JSON.stringify(state)); } catch (e) { } }
    function flag(k, v) { try { if (v === undefined) return sessionStorage.getItem(k); sessionStorage.setItem(k, v); } catch (e) { } return null; }
    function newSid() {
        var a = new Uint8Array(12), s = '';
        (window.crypto || window.msCrypto).getRandomValues(a);
        for (var i = 0; i < a.length; i++) s += ('0' + a[i].toString(16)).slice(-2);
        return s;
    }

    var state = read() || { sid: newSid(), msgs: [], open: false };
    var maxChars = 500, busy = false;
    var el = {};

    function esc(s) { return String(s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }

    // Phone numbers become tap-to-call links, and /service becomes a link to the request form.
    function rich(text) {
        var h = esc(text);
        h = h.replace(/(\+?964\s?7\d{2}[\s-]?\d{3}[\s-]?\d{4}|07\d{2}[\s-]?\d{3}[\s-]?\d{4})/g, function (m) {
            var d = m.replace(/[^\d]/g, '');
            if (d.indexOf('0') === 0) d = '964' + d.slice(1);
            return '<a href="tel:+' + d + '" dir="ltr">' + m + '</a>';
        });
        h = h.replace(/(^|[\s(])(\/service)(?=$|[\s).،,])/g, '$1<a href="/service">$2</a>');
        return h.replace(/\n/g, '<br>');
    }

    var ICON = {
        orion: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="7" cy="4.5" r="1.6"/><circle cx="17.5" cy="5.5" r="1.3"/><circle cx="9.6" cy="12.6" r="1.5"/><circle cx="12" cy="11.6" r="1.5"/><circle cx="14.4" cy="10.6" r="1.5"/><circle cx="6.5" cy="19.5" r="1.3"/><circle cx="17" cy="19" r="1.6"/></svg>',
        send: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12h13M13 6l6 6-6 6"/></svg>',
        close: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg>',
        renew: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5"/></svg>',
        phone: '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M6.6 10.8a15.1 15.1 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.25c1.1.37 2.3.57 3.6.57a1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1c0 1.25.2 2.45.57 3.57a1 1 0 0 1-.25 1L6.6 10.8z"/></svg>',
        wa: '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.3A10 10 0 1 0 12 2zm0 18.2a8.2 8.2 0 0 1-4.2-1.15l-.3-.18-3 .78.8-2.9-.2-.3A8.2 8.2 0 1 1 12 20.2zm4.5-6.1c-.25-.12-1.45-.72-1.68-.8-.22-.08-.39-.12-.55.12-.16.25-.63.8-.78.97-.14.16-.29.18-.53.06a6.7 6.7 0 0 1-3.3-2.9c-.25-.43.25-.4.72-1.33.08-.16.04-.3-.02-.43-.06-.12-.55-1.33-.76-1.82-.2-.48-.4-.41-.55-.42h-.47c-.16 0-.43.06-.65.3-.22.25-.86.84-.86 2.05 0 1.2.88 2.37 1 2.53.12.16 1.73 2.64 4.2 3.7 1.56.67 2.17.73 2.95.61.47-.07 1.45-.6 1.66-1.17.2-.57.2-1.06.14-1.17-.06-.1-.22-.16-.47-.28z"/></svg>',
        check: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5"/></svg>'
    };

    function bubble(role, text, cls) {
        var d = document.createElement('div');
        d.className = 'mc-msg mc-' + role + (cls ? ' ' + cls : '');
        d.setAttribute('dir', 'auto');
        d.innerHTML = role === 'model' ? rich(text) : esc(text).replace(/\n/g, '<br>');
        el.log.appendChild(d);
        return d;
    }

    // The saved booking, with the two ways to reach the workshop right now.
    function bookingCard(b) {
        var d = document.createElement('div');
        d.className = 'mc-booking';
        d.innerHTML =
            '<div class="mc-booking-head">' + ICON.check + '<b>' + esc(t('saved')) + '</b><span dir="ltr">' + esc(b.id) + '</span></div>' +
            '<p dir="auto">' + esc(b.name) + ' · <bdi dir="ltr">' + esc(b.phone) + '</bdi>' + (b.preferredTime ? '<br>' + esc(t('when')) + ': ' + esc(b.preferredTime) : '') + '</p>' +
            '<p class="mc-booking-note">' + esc(t('savedNote')) + '</p>' +
            '<div class="mc-booking-actions">' +
                '<a class="mc-act mc-act-wa" href="' + esc(b.whatsapp) + '" target="_blank" rel="noopener">' + ICON.wa + '<span>' + esc(t('sendWa')) + '</span></a>' +
                '<a class="mc-act" href="' + esc(b.call || TEL) + '">' + ICON.phone + '<span>' + esc(t('call')) + '</span></a>' +
            '</div>';
        el.log.appendChild(d);
    }

    function render() {
        el.log.innerHTML = '';
        bubble('model', t('hello'), 'mc-hello');
        state.msgs.forEach(function (m) {
            if (m.booking) bookingCard(m.booking);
            else bubble(m.role, m.text, m.err ? 'mc-err' : '');
        });
        el.chips.hidden = state.msgs.length > 0;
        scroll();
    }

    function paint() {
        el.btnLabel.textContent = t('open');
        el.btn.setAttribute('aria-label', t('open'));
        el.title.textContent = t('name');
        el.sub.textContent = t('sub');
        el.input.placeholder = t('ph');
        el.send.setAttribute('aria-label', t('send'));
        el.close.setAttribute('aria-label', t('close'));
        el.clear.setAttribute('aria-label', t('clear'));
        el.clear.title = t('clear');
        el.callBtn.setAttribute('aria-label', t('call')); el.callBtn.title = t('call');
        el.waBtn.setAttribute('aria-label', t('wa')); el.waBtn.title = t('wa');
        el.note.textContent = t('note');
        el.chips.innerHTML = '';
        t('chips').forEach(function (c, i) {
            var b = document.createElement('button');
            b.type = 'button'; b.className = 'mc-chip' + (i === 0 ? ' mc-chip-main' : ''); b.textContent = c;
            b.addEventListener('click', function () { ask(c); });
            el.chips.appendChild(b);
        });
        if (el.teaser) paintTeaser();
        render();
        count();
    }

    function scroll() { el.log.scrollTop = el.log.scrollHeight; }
    function count() {
        var n = el.input.value.length;
        el.count.textContent = n > maxChars * 0.8 ? (n + ' / ' + maxChars) : '';
        el.send.disabled = busy || n === 0 || n > maxChars;
    }
    function grow() { el.input.style.height = 'auto'; el.input.style.height = Math.min(el.input.scrollHeight, 120) + 'px'; }

    function setOpen(open) {
        state.open = open;
        save();
        hideTeaser();
        el.wrap.classList.toggle('mc-open', open);
        el.btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        el.panel.hidden = !open;
        document.body.classList.toggle('mc-chat-open', open);
        if (open) { render(); setTimeout(function () { el.input.focus(); }, 60); }
    }

    function ask(text) {
        text = String(text || '').trim();
        if (!text || busy) return;
        if (text.length > maxChars) text = text.slice(0, maxChars);
        var history = state.msgs.filter(function (m) { return !m.err && !m.booking; }).slice(-HISTORY).map(function (m) { return { role: m.role, text: m.text }; });
        state.msgs.push({ role: 'user', text: text });
        bubble('user', text);
        el.chips.hidden = true;
        el.input.value = ''; grow();
        busy = true; count();
        var typing = bubble('model', t('typing'), 'mc-typing');
        scroll();

        fetch('/api/chat', {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
            body: JSON.stringify({ message: text, history: history, lang: lang(), sid: state.sid, page: location.pathname })
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (d) { return { ok: r.ok, d: d }; });
        }).then(function (res) {
            typing.remove();
            if (res.ok && res.d.reply) {
                state.msgs.push({ role: 'model', text: res.d.reply });
                bubble('model', res.d.reply);
                if (res.d.booking) { state.msgs.push({ role: 'model', booking: res.d.booking }); bookingCard(res.d.booking); }
            } else {
                var msg = (res.d && res.d.reply) || t('err');
                state.msgs.push({ role: 'model', text: msg, err: true });
                bubble('model', msg, 'mc-err');
            }
        }).catch(function () {
            typing.remove();
            state.msgs.push({ role: 'model', text: t('err'), err: true });
            bubble('model', t('err'), 'mc-err');
        }).then(function () {
            if (state.msgs.length > MAX_SHOWN) state.msgs = state.msgs.slice(-MAX_SHOWN);
            save();
            busy = false; count(); scroll();
            el.input.focus();
        });
    }

    // ---- the greeting a visitor sees on arrival (once per visit) -------------
    function paintTeaser() {
        el.teaser.querySelector('.mc-teaser-text').textContent = t('teaser');
        el.teaser.querySelector('.mc-teaser-name').textContent = t('name');
        el.teaser.querySelector('[data-t="book"]').lastChild.textContent = t('book');
        el.teaser.querySelector('[data-t="chat"]').textContent = t('chat');
        el.teaser.querySelector('[data-t="wa"]').lastChild.textContent = t('wa');
        el.teaser.querySelector('.mc-teaser-x').setAttribute('aria-label', t('dismiss'));
    }
    function showTeaser() {
        if (state.open || el.teaser || flag(GREETED_KEY)) return;
        flag(GREETED_KEY, '1');
        var d = document.createElement('div');
        d.className = 'mc-teaser';
        d.setAttribute('role', 'status');
        d.innerHTML =
            '<button type="button" class="mc-teaser-x">' + ICON.close + '</button>' +
            '<div class="mc-teaser-who"><span class="mc-avatar">' + ICON.orion + '</span><b class="mc-teaser-name"></b></div>' +
            '<p class="mc-teaser-text" dir="auto"></p>' +
            '<div class="mc-teaser-actions">' +
                '<button type="button" class="mc-act mc-act-main" data-t="book">' + ICON.check + '<span></span></button>' +
                '<a class="mc-act mc-act-wa" data-t="wa" href="' + WA + '" target="_blank" rel="noopener">' + ICON.wa + '<span></span></a>' +
                '<button type="button" class="mc-act mc-act-line" data-t="chat"></button>' +
            '</div>';
        el.wrap.insertBefore(d, el.btn);
        el.teaser = d;
        paintTeaser();
        d.querySelector('.mc-teaser-x').addEventListener('click', hideTeaser);
        d.querySelector('[data-t="chat"]').addEventListener('click', function () { setOpen(true); });
        d.querySelector('[data-t="book"]').addEventListener('click', function () { setOpen(true); ask(t('bookMsg')); });
    }
    function hideTeaser() { if (el.teaser) { el.teaser.remove(); el.teaser = null; } }

    function build() {
        var wrap = document.createElement('div');
        wrap.className = 'me-chat';
        wrap.innerHTML =
            '<div class="mc-panel" id="mcPanel" role="dialog" aria-labelledby="mcTitle" hidden>' +
                '<header class="mc-head">' +
                    '<span class="mc-avatar" aria-hidden="true">' + ICON.orion + '</span>' +
                    '<span class="mc-heading"><strong id="mcTitle"></strong><small></small></span>' +
                    '<a class="mc-icon mc-call" href="' + TEL + '">' + ICON.phone + '</a>' +
                    '<a class="mc-icon mc-wa" href="' + WA + '" target="_blank" rel="noopener">' + ICON.wa + '</a>' +
                    '<button type="button" class="mc-icon mc-clear">' + ICON.renew + '</button>' +
                    '<button type="button" class="mc-icon mc-close">' + ICON.close + '</button>' +
                '</header>' +
                '<div class="mc-log" role="log" aria-live="polite"></div>' +
                '<div class="mc-chips"></div>' +
                '<form class="mc-form" novalidate>' +
                    '<textarea rows="1" dir="auto" autocomplete="off"></textarea>' +
                    '<button type="submit" class="mc-send">' + ICON.send + '</button>' +
                '</form>' +
                '<p class="mc-foot"><span class="mc-note"></span><span class="mc-count" aria-live="polite"></span></p>' +
            '</div>' +
            '<button type="button" class="mc-launch" aria-controls="mcPanel" aria-expanded="false"><span class="mc-avatar" aria-hidden="true">' + ICON.orion + '</span><span class="mc-launch-label"></span></button>';
        document.body.appendChild(wrap);

        el = {
            wrap: wrap, panel: wrap.querySelector('.mc-panel'), btn: wrap.querySelector('.mc-launch'), btnLabel: wrap.querySelector('.mc-launch-label'),
            title: wrap.querySelector('.mc-heading strong'), sub: wrap.querySelector('.mc-heading small'),
            log: wrap.querySelector('.mc-log'), chips: wrap.querySelector('.mc-chips'), form: wrap.querySelector('.mc-form'),
            input: wrap.querySelector('textarea'), send: wrap.querySelector('.mc-send'), close: wrap.querySelector('.mc-close'),
            clear: wrap.querySelector('.mc-clear'), callBtn: wrap.querySelector('.mc-call'), waBtn: wrap.querySelector('.mc-wa'),
            note: wrap.querySelector('.mc-note'), count: wrap.querySelector('.mc-count'), teaser: null
        };
        el.input.maxLength = maxChars;

        el.btn.addEventListener('click', function () { setOpen(!state.open); });
        el.close.addEventListener('click', function () { setOpen(false); el.btn.focus(); });
        el.clear.addEventListener('click', function () { if (busy) return; state = { sid: newSid(), msgs: [], open: true }; save(); render(); el.input.focus(); });
        el.form.addEventListener('submit', function (e) { e.preventDefault(); ask(el.input.value); });
        el.input.addEventListener('input', function () { count(); grow(); });
        el.input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); ask(el.input.value); }
        });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && state.open) { setOpen(false); el.btn.focus(); } });
        document.addEventListener('me:lang', paint);

        paint();
        if (state.open) setOpen(true);

        // Greet the visitor: right after they pick a language, or shortly after the page opens.
        if (document.body.classList.contains('lang-gate-open')) document.addEventListener('me:lang-chosen', function () { setTimeout(showTeaser, 600); });
        else if (window.ME && window.ME.chosen) setTimeout(showTeaser, 600);
        else setTimeout(showTeaser, 1500);
    }

    function start() {
        fetch('/api/chat/config', { credentials: 'same-origin' })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (c) {
                if (!c || !c.enabled) return;           // no key, or switched off in the dashboard
                maxChars = c.maxChars || maxChars;
                build();
            })
            .catch(function () { });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
})();
