/* ME Workshop — home page: hero slides, the timeline, the map of Iraq, the visit map. */
(function () {
    'use strict';
    var C = window.ME_CONTENT || {};
    var ME = window.ME;
    var BASE = (document.currentScript && document.currentScript.src.replace(/home\.js.*$/, '')) || '/me/';
    var V = ((document.currentScript && document.currentScript.src.split('?v=')[1]) || '1'); // same version as home.js, so maps.js is never stale
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var IRAQ = 'M309.5,21.4L309,23.2L309.3,24.9L310.4,25.5L311.1,27.1L312.8,27.2L314.2,27.9L315.3,28.5L315.6,29.7L314.7,32L315.5,34L315.6,35.3L315.5,36.7L314.7,37.9L312.9,40L312.4,41.4L313.8,43.1L315.1,43.4L316.7,43.3L318,43.9L319.1,44.9L320.3,45.1L321.8,46.5L323.1,48.6L323.6,49.9L322.5,53.2L322,55.3L322,56.5L320.9,58L321.8,59L323.2,60.4L323.6,61.7L324.5,62.7L324.7,64L325.6,65.2L326.9,65.8L328.3,65.7L331.2,64.6L332.5,64.7L333.4,65.8L333.9,67.3L334.1,68.7L334.3,71.3L335.5,73.5L334.3,74.2L335.5,75.7L336.4,77.6L336.4,80.3L336.6,81.5L338.2,83.6L338.8,84.8L338.6,86L337.3,88.1L337.2,89.3L338.2,91.3L340.4,91.1L341.5,90.3L344,89.3L345.5,89.2L348.4,90.3L350.3,92.1L352.3,92.7L353.8,94L356.1,97.2L357.4,100.3L358.8,101.4L360.8,100.8L362.3,101.5L364.3,100.9L365.4,99.9L367,100.2L368.6,99.6L371.9,99.8L373.7,98.9L375.5,98.6L376.9,99.2L377.8,100.7L378.7,101.7L379.8,102.2L382.9,101.7L384.7,100.8L386.7,100.4L387.9,101.8L387.2,103.7L384.9,105.3L384.3,106.4L382.3,107.2L380.5,107.1L378,108.4L373.3,108.5L371.7,109.6L371.1,111.5L371.7,114.9L369.9,115.2L369.9,116.5L370.4,117.6L370.1,118.9L369.7,120.3L370.4,122.1L373.7,127.1L376.4,129.6L377.6,130.9L378,132.7L376.9,134.2L376.6,135.9L379.3,136.4L379.8,138.6L378.7,140.4L378,142.1L379.1,143.3L378.2,144.3L376.2,144.9L374.9,144.6L373.5,145.5L372,146.4L370.4,145.7L368.2,144.9L366.4,145.9L366.4,147.2L365.2,147.5L364.3,148.7L364.4,150.3L365,151.9L364.8,153.1L364.1,155.6L363.2,156.6L361.9,156.1L360.5,155.4L359.2,155.4L358.8,156.6L359,158.1L358.7,159.5L357.4,160.5L356,160.7L354.9,161.6L354.7,163.5L353.2,165.8L353.6,167.5L354.9,168.6L355,169.8L355.8,170.8L356,173.1L356.7,174.8L356.7,176.6L355,176.6L351.6,176.4L350.2,176L348.4,175L346.4,174.5L346.2,176.2L346.7,178.6L346.6,180.3L345.8,181.6L342.8,182.5L343.8,188.3L344.4,189.5L345.5,190.2L346.4,189.4L348.5,190.1L349.3,191.3L349.6,192.7L349.4,194.1L348.9,197L347.8,198.6L348,200L348.7,201L348.2,202.3L346.2,203.9L344.2,205.8L343.7,207.2L343.1,208.7L342.4,210.3L341.5,211.1L340.4,211.6L341.5,213L342.6,213.6L343.8,213.7L345.5,213.5L350.2,221.7L351.6,223.2L352.7,224.9L352.9,226.1L353.8,228.1L355.2,229.9L357.6,231.6L357.9,234.6L359.9,234.2L361.4,232.8L364.6,232.4L365.5,234L366.4,234.9L367.5,235.6L367.9,236.9L365,239.3L364.1,240.3L365,241.1L366.4,241.4L368.1,241.2L369,240.4L370.2,240.6L371.9,242.6L372.6,243.7L373.1,246.1L373.3,247.9L375.1,249.5L377.1,252.3L378.5,253.7L379.8,255.4L379.4,257.2L380.2,258.6L380,259.9L377.8,262.1L376.7,262.9L375.1,263L373.1,263.7L373.7,265L375.8,265.2L377.6,266.3L377.5,268.2L376,269.6L376.4,272.7L379.3,273.1L385.2,272.4L390.7,274.1L395.5,276.5L397,277.9L401.7,280.6L404.2,282.6L407.5,284.7L409.6,287L424.6,300.4L426.3,301.5L427.9,302.3L429.3,302.7L431.1,302.5L434.4,300.9L437.8,301.9L438.9,302.5L440.2,304.1L441.1,305.2L442.3,306.2L442.7,307.5L441.8,309.3L442,310.6L443.6,312.4L445.1,314.2L446.1,315.7L447.2,317.2L446.9,318.6L446.3,320.6L447.6,321.5L448.5,322.7L449.8,323.3L451.6,324.9L452.8,327.5L453.6,328.4L455.4,329.2L455.7,330.4L457.7,334.6L459,335.7L460.1,337.7L461,339.1L463,340.6L463.7,341.7L463.5,344.3L455.9,365.5L455.7,375.8L455.5,390.3L472,390.3L472.7,391.5L472.5,420.3L472.5,422.1L478,423L479.8,424.4L480.5,425.6L481,426.9L481.4,429.5L481.9,430.6L483.2,431L485.5,430.6L486.8,430.9L488.3,433L489.9,434.9L491.9,436.7L492.4,437.9L492.4,439.2L491.5,440.1L491.1,441.3L491.9,443.1L492.8,444.2L494.2,448L494.8,449.9L496,451.2L498.2,452.1L499.8,452.6L499.5,454L498,454.3L492.6,453.7L488.8,452.3L487,450.9L485.2,450.3L482.5,448.5L480.3,447.8L478,447.3L475.8,447.3L473.8,447.8L470.4,449.7L470,448.2L469.8,446.4L469.5,444.7L468.7,443.5L469.3,445.6L469.3,447.1L468.9,448.4L469.3,450.3L458.4,444.6L455.5,444.1L442.5,444.1L439.6,444.4L431.7,448L429,449.8L427.3,452.3L423,463.7L421.2,467.2L420.7,469.9L419.6,472.8L415.8,479.3L414.3,483.3L413.6,484.5L410.4,488.2L407.3,493.7L399.7,502.6L398.2,504.2L396.1,504.7L393.9,505.2L391.7,505.7L389.4,506.2L384.1,505.7L380.4,505.3L376.6,505L372.8,504.6L369,504.2L363.7,503.8L358.7,503.3L353.4,502.8L348.4,502.3L343.1,501.8L338.1,501.4L332.8,500.9L327.8,500.4L323.8,500L319.8,499.7L315.6,499.2L311.7,498.8L307.2,498.4L305.9,497.9L301.9,494.6L297.2,490.6L292.3,486.6L287.6,482.5L282.9,478.5L277.1,473.6L271.4,468.8L265.6,464L262.3,461.2L259.8,459.1L254,454.3L248.4,449.4L242.6,444.5L236.8,439.7L232.3,436L228,432.3L223.6,428.5L219.1,424.8L213.9,420.3L210.1,417L209,416.1L204.3,411.8L199.6,407.6L194.7,403.3L190.6,399.5L186.2,395.7L182.1,392L177.7,388.2L174.5,385.2L170,382.4L165.6,379.8L161.1,377.2L156.8,374.4L152.4,371.8L147.9,369.1L143.6,366.4L139.2,363.7L134.7,361.1L130.4,358.3L126,355.7L121.5,353L117.2,350.3L112.7,347.6L108.3,344.9L104,342.2L99.5,339.6L94.4,336.4L91.5,334.8L88.8,333.7L71.8,330.4L67.7,329.6L57.7,327.9L47.8,326.2L37.8,324.5L28.1,322.8L33.5,317.2L34.8,315.4L33.7,311.3L33,309.5L31.9,308.8L22.5,311.5L21.6,310.3L19,301.7L23,300.2L19.6,287.7L17.2,278.9L14.9,270.4L13.3,264L11.1,256.3L8.7,247.7L14.3,244.3L20,241.1L25.4,237.8L31,234.4L36.6,231.1L42,227.7L47.6,224.4L53.2,221.1L58.8,217.8L64.2,214.5L69.8,211.1L75.4,207.8L79.1,205.6L80.9,204.5L86.5,201.2L92.1,197.8L97.7,194.5L104.9,190.1L117.4,186.8L118.8,185.9L119.9,184.3L121.7,180.3L130.4,163.9L130.7,162.4L130.9,160.9L130.4,150.3L130.2,140.5L130,139.1L130.6,135.4L132.7,128L133.1,122.2L133.6,120.3L136,116.9L137.6,114.4L138.5,112.5L138.7,110.7L138.5,102.4L138.3,100.5L137.8,98.6L133.8,90.3L132.6,87.4L132.4,85.4L134,70.3L134.4,68.7L138.9,60.4L139.8,59L141.4,58.3L144.7,57.8L160.2,54.7L161.5,54L162.9,53L169.6,46L179.7,35.6L184.8,30.4L188,27.4L189.7,26.3L189.3,24.7L188.6,23.4L190.9,23.2L193.8,22.3L198.2,21.6L199.1,20.8L199.6,19.2L205.9,10.5L206.3,9.2L209.4,7.5L211,7.9L211.5,9.2L215.7,10.5L217.7,10.8L219.9,10.1L221.1,9.2L223.1,8.4L225.1,7.9L226.7,7.8L234.1,11.3L235.4,11.4L237.2,10.7L239.2,11.7L241.9,13.2L244.3,15.1L245.7,15.3L247.5,14.8L249.5,15.8L250.8,16.3L252,16.4L257.1,16.1L258.4,16.2L259.6,16.5L261.2,17.8L263.1,17L265.8,16.5L267.4,14.9L268.8,12.7L270.6,11.2L272.8,10.9L274.6,11.2L280.4,13.2L281.5,14L282.4,14.8L283.3,17.2L283.7,18.5L283.7,19.8L282.8,20.7L281.5,21.2L280.6,22.3L280.2,23.5L280.2,24.8L280.6,26L282.6,30.4L283.3,31.4L285.5,31.9L286.9,30.4L287.8,29L288.4,27.4L292.7,26.1L294,25.4L295.2,24.5L296.3,23L298.1,21.3L300.1,20L301.7,19.3L307.9,19.9L309,20.4Z';

    function esc(s) {
        return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
    function lang() { return (ME && ME.lang) || 'ar'; }

    // ---- hero: a slow crossfade between the photographs --------------------
    (function () {
        var slides = document.querySelectorAll('.hero-slide');
        if (slides.length < 2 || reduce) return;
        var i = 0;
        setInterval(function () {
            if (document.hidden) return;
            slides[i].classList.remove('active');
            i = (i + 1) % slides.length;
            slides[i].classList.add('active');
        }, 7000);
    })();

    // ---- the timeline ------------------------------------------------------
    var rail = document.getElementById('rail');
    function yearOf(item) { var n = parseInt(item.year, 10); return isNaN(n) ? 0 : n; }
    function caption(item) {
        var l = lang();
        return window.ME.pick(item, 'caption', l) || item.captionAr || item.captionEn || '';
    }
    function paintRail() {
        if (!rail) return;
        var items = (C.archive || []).slice().sort(function (a, b) { return yearOf(a) - yearOf(b); });
        if (!items.length) { rail.innerHTML = ''; return; }
        rail.innerHTML = items.map(function (item) {
            var y = yearOf(item);
            var age = y && y < 1970 ? 'old' : y && y < 2000 ? 'mid' : 'new';
            var photo = item.imageUrl
                ? '<div class="era-photo ' + age + '"><img src="' + esc(item.imageUrl) + '" alt="" loading="lazy" onerror="this.remove()" /></div>'
                : '';
            return '<article class="era">' +
                '<div class="era-rule"><span class="ticks"></span></div>' +
                '<span class="era-year">' + esc(item.year) + '</span>' + photo +
                '<p>' + esc(caption(item)) + '</p></article>';
        }).join('');
    }
    function slide(dir) {
        if (!rail) return;
        var card = rail.querySelector('.era');
        var step = card ? card.getBoundingClientRect().width : 320;
        var rtl = document.documentElement.dir === 'rtl';
        rail.scrollBy({ left: dir * step * (rtl ? -1 : 1), behavior: reduce ? 'auto' : 'smooth' });
    }
    var prev = document.getElementById('railPrev'), next = document.getElementById('railNext');
    if (prev) prev.addEventListener('click', function () { slide(-1); });
    if (next) next.addEventListener('click', function () { slide(1); });

    // ---- Iraq, with Najaf at the centre of it -------------------------------
    var K = Math.cos(33.2 * Math.PI / 180) * 60;
    function px(lat, lon) { return [(lon - 38.6) * K, (37.5 - lat) * 60]; }
    function smooth(points) {
        var p = points.map(function (q) { return px(q[0], q[1]); });
        var d = 'M' + p[0][0].toFixed(1) + ',' + p[0][1].toFixed(1);
        for (var i = 1; i < p.length - 1; i++) {
            var mx = (p[i][0] + p[i + 1][0]) / 2, my = (p[i][1] + p[i + 1][1]) / 2;
            d += 'Q' + p[i][0].toFixed(1) + ',' + p[i][1].toFixed(1) + ' ' + mx.toFixed(1) + ',' + my.toFixed(1);
        }
        var last = p[p.length - 1];
        return d + 'L' + last[0].toFixed(1) + ',' + last[1].toFixed(1);
    }
    var EUPHRATES = [[34.42, 40.95], [34.37, 41.98], [34.13, 42.38], [33.64, 42.83], [33.43, 43.3], [33.35, 43.78], [32.78, 44.29], [32.03, 44.4], [31.96, 44.6], [31.6, 45.0], [31.31, 45.28], [31.04, 46.26], [31.0, 47.43]];
    var TIGRIS = [[37.05, 42.38], [36.34, 43.13], [35.8, 43.29], [34.93, 43.49], [34.6, 43.68], [34.2, 43.87], [33.31, 44.4], [32.9, 45.0], [32.51, 45.82], [31.84, 47.14], [31.0, 47.43]];
    var SHATT = [[31.0, 47.43], [30.51, 47.82], [29.97, 48.47]];
    var NAJAF = { lat: 32.0, lon: 44.34, ar: 'النجف', en: 'Najaf', ku: 'نەجەف', fa: 'نجف', tr: 'Necef' };
    // lat, lon, names (ar, en, ku), which side the label sits on (1 right, -1 left), vertical nudge, names (fa, tr)
    var CITIES = [
        [36.87, 42.99, 'دهوك', 'Duhok', 'دهۆک', -1, 0, 'دهوک', 'Duhok'], [36.34, 43.13, 'الموصل', 'Mosul', 'مووسڵ', -1, 4, 'موصل', 'Musul'],
        [36.19, 44.01, 'أربيل', 'Erbil', 'هەولێر', 1, 0, 'اربیل', 'Erbil'], [35.56, 45.43, 'السليمانية', 'Sulaymaniyah', 'سلێمانی', 1, 12, 'سلیمانیه', 'Süleymaniye'],
        [35.47, 44.39, 'كركوك', 'Kirkuk', 'کەرکووک', -1, 10, 'کرکوک', 'Kerkük'], [34.6, 43.68, 'تكريت', 'Tikrit', 'تکریت', -1, 3, 'تکریت', 'Tikrit'],
        [33.75, 44.64, 'بعقوبة', 'Baqubah', 'بەعقووبە', 1, -3, 'بعقوبه', 'Bakuba'], [33.31, 44.37, 'بغداد', 'Baghdad', 'بەغدا', 1, 10, 'بغداد', 'Bağdat'],
        [33.42, 43.3, 'الرمادي', 'Ramadi', 'ڕەمادی', -1, 12, 'رمادی', 'Ramadi'], [32.61, 44.02, 'كربلاء', 'Karbala', 'کەربەلا', -1, 0, 'کربلا', 'Kerbela'],
        [32.48, 44.43, 'الحلة', 'Hillah', 'حیللە', 1, -2, 'حله', 'Hille'], [32.51, 45.82, 'الكوت', 'Kut', 'کووت', 1, -4, 'کوت', 'Kut'],
        [31.99, 44.93, 'الديوانية', 'Diwaniyah', 'دیوانیە', 1, 4, 'دیوانیه', 'Divaniye'], [31.31, 45.28, 'السماوة', 'Samawah', 'سەماوە', -1, 12, 'سماوه', 'Semave'],
        [31.04, 46.26, 'الناصرية', 'Nasiriyah', 'ناسریە', -1, 14, 'ناصریه', 'Nasıriye'], [31.84, 47.14, 'العمارة', 'Amarah', 'عەمارە', 1, -4, 'عماره', 'Amare'],
        [30.51, 47.78, 'البصرة', 'Basra', 'بەسرە', -1, 4, 'بصره', 'Basra']
    ];
    function paintIraq() {
        var box = document.getElementById('iraqMap');
        if (!box) return;
        var l = lang(), col = { en: 3, ku: 4, fa: 7, tr: 8 }[l] || 2;
        var n = px(NAJAF.lat, NAJAF.lon);
        var routes = '', cities = '';
        CITIES.forEach(function (c, i) {
            var p = px(c[0], c[1]);
            var mx = (p[0] + n[0]) / 2, my = (p[1] + n[1]) / 2;
            var dx = n[0] - p[0], dy = n[1] - p[1];
            var cx = mx - dy * 0.16, cy = my + dx * 0.16;
            routes += '<path class="route" style="--i:' + i + '" d="M' + p[0].toFixed(1) + ',' + p[1].toFixed(1) + 'Q' + cx.toFixed(1) + ',' + cy.toFixed(1) + ' ' + n[0].toFixed(1) + ',' + n[1].toFixed(1) + '"/>';
            cities += '<g class="city"><circle cx="' + p[0].toFixed(1) + '" cy="' + p[1].toFixed(1) + '" r="2.4"/>' +
                '<text x="' + (p[0] + c[5] * 7).toFixed(1) + '" y="' + (p[1] + 3.5 + c[6]).toFixed(1) + '" text-anchor="' + (c[5] > 0 ? 'start' : 'end') + '" style="direction:ltr">' + esc(c[col]) + '</text></g>';
        });
        var e = px(34.05, 41.75), t = px(33.95, 44.0);
        box.innerHTML = '<svg class="iraq" viewBox="-6 6 524 520" role="img" aria-label="' + esc(ME.text('reachTitle')) + '">' +
            '<path class="land" d="' + IRAQ + '"/>' +
            '<path class="river" d="' + smooth(EUPHRATES) + '"/><path class="river" d="' + smooth(TIGRIS) + '"/><path class="river" d="' + smooth(SHATT) + '"/>' +
            '<text class="river-name" x="' + e[0].toFixed(1) + '" y="' + (e[1] + 16).toFixed(1) + '" text-anchor="middle">' + esc(ME.text('riverEuphrates')) + '</text>' +
            '<text class="river-name" x="' + (t[0] + 10).toFixed(1) + '" y="' + t[1].toFixed(1) + '" text-anchor="start" style="direction:ltr">' + esc(ME.text('riverTigris')) + '</text>' +
            routes + cities +
            '<g class="home"><circle class="ring" cx="' + n[0].toFixed(1) + '" cy="' + n[1].toFixed(1) + '" r="7"/><circle class="dot" cx="' + n[0].toFixed(1) + '" cy="' + n[1].toFixed(1) + '" r="5"/>' +
            '<text x="' + (n[0] - 12).toFixed(1) + '" y="' + (n[1] + 20).toFixed(1) + '" text-anchor="end" style="direction:ltr">' + esc(NAJAF[l] || NAJAF.ar) + '</text></g>' +
            '</svg>';
        var svg = box.querySelector('svg');
        if (!('IntersectionObserver' in window) || reduce) { svg.classList.add('in'); return; }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) { if (en.isIntersecting) { svg.classList.add('in'); io.disconnect(); } });
        }, { threshold: 0.3 });
        io.observe(svg);
    }

    // ---- the visit map: loaded only when the visitor gets near it ------------
    var map = null, marker = null;
    function load(src, done) {
        var s = document.createElement('script'); s.src = src; s.onload = done; document.head.appendChild(s);
    }
    function startMap() {
        var el = document.getElementById('homeMap');
        if (!el || map) return;
        var css = document.createElement('link'); css.rel = 'stylesheet'; css.href = BASE + 'leaflet/leaflet.css'; document.head.appendChild(css);
        load(BASE + 'leaflet/leaflet.js', function () {
            load(BASE + 'maps.js?v=' + V, function () {
                var street = MEMaps.street(), imagery = MEMaps.imagery();
                map = L.map(el, { center: MEMaps.center, zoom: 16, layers: [street], scrollWheelZoom: false, zoomControl: true });
                L.control.layers({ 'Map': street, 'Satellite': imagery }, null, { position: 'topright', collapsed: true }).addTo(map);
                marker = MEMaps.addWorkshop(map, MEMaps.center[0], MEMaps.center[1], ME.text('mapPinTitle'));
                MEMaps.locateControl(map, function () { return marker; });
                map.on('click', function () { map.scrollWheelZoom.enable(); });
                setTimeout(function () { map.invalidateSize(); }, 200);
            });
        });
    }
    (function () {
        var el = document.getElementById('visit');
        if (!el) return;
        if (!('IntersectionObserver' in window)) { startMap(); return; }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) { if (en.isIntersecting) { startMap(); io.disconnect(); } });
        }, { rootMargin: '600px 0px' });
        io.observe(el);
    })();

    function paint() {
        paintRail();
        paintIraq();
        if (marker) {
            marker.setPopupContent(MEMaps.workshopPopup(MEMaps.center[0], MEMaps.center[1], ME.text('mapPinTitle')));
            MEMaps.renameWorkshop(marker, ME.text('mapPinTitle'));
        }
    }
    document.addEventListener('me:lang', paint);
    // Deferred scripts run before DOMContentLoaded; waiting for it lets the page's own script go first.
    if (document.readyState !== 'complete') document.addEventListener('DOMContentLoaded', paint); else paint();
})();
