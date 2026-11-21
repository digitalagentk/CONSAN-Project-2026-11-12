/* ME Workshop — shared map settings.
   The workshop position and the pan area come from the server (Pages.MapConfig): the
   workshop pin edited in the dashboard, and the Industrial District grown to include
   every approved location. Public maps are locked to that area; dashboard maps are
   created with { meFree: true } so staff can place a pin anywhere.
   Street tiles: OpenStreetMap. Imagery: Esri. */
(function () {
    var C = window.ME_MAP || window.ME_CONTENT || {};
    var center = C.pin || [32.0118, 44.3775];
    var box = C.bounds || [[32.0030, 44.3655], [32.0225, 44.3905]];

    var OSM = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
    var ESRI = 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}';
    var OSM_CREDIT = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
    var ESRI_CREDIT = 'Imagery &copy; Esri, Maxar, Earthstar Geographics';

    function street() { return L.tileLayer(OSM, { maxZoom: 19, attribution: OSM_CREDIT, className: 'tiles-street' }); }
    function imagery() { return L.tileLayer(ESRI, { maxZoom: 19, maxNativeZoom: 18, attribution: ESRI_CREDIT, className: 'tiles-imagery' }); }

    window.MEMaps = {
        center: center,
        bounds: box,
        street: street,
        imagery: imagery,
        // Kept for the pages that used to ask for Google layer types (m, s, y, p).
        layer: function (type) { return (type === 's' || type === 'y') ? imagery() : street(); },
        pinIcon: function () { return L.divIcon({ className: '', html: '<span class="me-pin"></span>', iconSize: [22, 22], iconAnchor: [11, 11], popupAnchor: [0, -12] }); },

        // The workshop itself: a large brass "ME" badge with a pulse, above every other marker.
        // The name label is part of the icon (not a Leaflet tooltip), so it stays centred above the badge in RTL too.
        workshopIcon: function (name) {
            injectCss();
            var label = name ? '<span class="me-hq-name" dir="auto">' + esc(name) + '</span>' : '';
            return L.divIcon({ className: 'me-hq-wrap', html: '<span class="me-hq-pulse"></span><span class="me-hq"><b>ME</b></span>' + label, iconSize: [48, 48], iconAnchor: [24, 24], popupAnchor: [0, -54] });
        },

        renameWorkshop: function (marker, name) { if (marker) marker.setIcon(window.MEMaps.workshopIcon(name)); },

        // Adds the workshop marker with a permanent name label and a popup with directions.
        addWorkshop: function (target, lat, lng, name, extraHtml) {
            var m = L.marker([lat, lng], { icon: window.MEMaps.workshopIcon(name), zIndexOffset: 1000, riseOnHover: true, title: name, alt: name });
            m.bindPopup(window.MEMaps.workshopPopup(lat, lng, name, extraHtml));
            m.addTo(target);
            return m;
        },

        workshopPopup: function (lat, lng, name, extraHtml) {
            var dir = 'https://www.google.com/maps/dir/?api=1&destination=' + lat + ',' + lng;
            return '<div class="me-hq-pop"><strong>' + esc(name) + '</strong>' + (extraHtml || '') +
                '<a class="me-hq-dir" href="' + dir + '" target="_blank" rel="noopener">' + esc(word('dir')) + '</a></div>';
        },

        // A map button that always brings the view back to the workshop.
        locateControl: function (map, getMarker) {
            injectCss();
            var Ctl = L.Control.extend({
                options: { position: 'topleft' },
                onAdd: function () {
                    var b = L.DomUtil.create('button', 'me-locate');
                    b.type = 'button';
                    b.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><circle cx="12" cy="12" r="3.2"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/><circle cx="12" cy="12" r="7.5"/></svg><span></span>';
                    b.lastChild.textContent = word('locate');
                    b.setAttribute('aria-label', word('locate'));
                    L.DomEvent.disableClickPropagation(b);
                    L.DomEvent.on(b, 'click', function () {
                        var m = getMarker();
                        if (!m) return;
                        map.flyTo(m.getLatLng(), 18, { duration: 0.8 });
                        setTimeout(function () { m.openPopup(); }, 850);
                    });
                    document.addEventListener('me:lang', function () { b.lastChild.textContent = word('locate'); b.setAttribute('aria-label', word('locate')); });
                    return b;
                }
            });
            return new Ctl().addTo(map);
        }
    };

    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function word(k) {
        var l = (window.ME && window.ME.lang) || document.documentElement.lang || 'ar';
        var W = { locate: { ar: 'موقع الورشة', en: 'Workshop location', ku: 'شوێنی کارگە' }, dir: { ar: 'الاتجاهات في خرائط Google', en: 'Directions in Google Maps', ku: 'ئاراستە لە Google Maps' } };
        return (W[k] && (W[k][l] || W[k].ar)) || '';
    }
    // Styles for the workshop marker live here so every page with a map (the dashboard too) gets them.
    var cssDone = false;
    function injectCss() {
        if (cssDone) return; cssDone = true;
        var st = document.createElement('style');
        st.textContent =
            '.me-hq-wrap{background:none;border:0}' +
            '.me-hq{position:absolute;inset:4px;display:grid;place-items:center;border-radius:12px;background:linear-gradient(135deg,#E6CD9E,#B8935A 45%,#E3CB98 62%,#A9844C);border:3px solid #17130B;box-shadow:0 0 0 2px #E6CD9E,0 10px 22px rgba(0,0,0,.55)}' +
            '.me-hq b{font:800 15px/1 "Barlow Condensed",Arial,sans-serif;letter-spacing:.04em;color:#17130B}' +
            '.me-hq-pulse{position:absolute;inset:0;border-radius:50%;background:rgba(230,205,158,.45);animation:meHqPulse 2s ease-out infinite}' +
            '@keyframes meHqPulse{0%{transform:scale(.6);opacity:.9}100%{transform:scale(1.9);opacity:0}}' +
            '@media (prefers-reduced-motion:reduce){.me-hq-pulse{animation:none;opacity:.35}}' +
            '.me-hq-name{position:absolute;bottom:calc(100% + 4px);left:50%;transform:translateX(-50%);white-space:nowrap;background:#17130B;color:#E6CD9E;border:1px solid #B8935A;border-radius:4px;font:700 13px/1.3 "Noto Kufi Arabic","Barlow Condensed",sans-serif;padding:3px 9px;box-shadow:0 6px 16px rgba(0,0,0,.4);pointer-events:none}' +
            '.me-hq-pop strong{display:block;font-size:14px;margin-bottom:4px}' +
            '.me-hq-dir{display:inline-block;margin-top:8px;padding:6px 10px;border-radius:4px;background:#B8935A;color:#17130B!important;font-weight:700;text-decoration:none}' +
            '.me-locate{display:inline-flex;align-items:center;gap:6px;padding:7px 11px;border:1px solid #B8935A;border-radius:6px;background:#17130B;color:#E6CD9E;font:700 13px/1.2 "Noto Kufi Arabic",sans-serif;cursor:pointer;box-shadow:0 6px 16px rgba(0,0,0,.35)}' +
            '.me-locate svg{width:16px;height:16px}' +
            '.me-locate:hover{background:#2a2216}';
        document.head.appendChild(st);
    }

    if (window.L && L.Marker) L.Marker.prototype.options.icon = window.MEMaps.pinIcon();

    if (window.L && L.Map) {
        L.Map.addInitHook(function () {
            if (this.attributionControl) this.attributionControl.setPrefix(false);
            if (this.options.meFree) return;
            var b = L.latLngBounds(box);
            this.setMaxBounds(b.pad(0.02));
            this.options.maxBoundsViscosity = 1;
            this.setMinZoom(15);
        });
    }
})();
