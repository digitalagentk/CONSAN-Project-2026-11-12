/* ME Workshop — shared map settings.
   Every map on the site is locked to Najaf's Industrial District, across the road
   from the University of Kufa. Street tiles: OpenStreetMap. Imagery: Esri. */
(function () {
    var C = window.ME_CONTENT || {};
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
        pinIcon: function () { return L.divIcon({ className: '', html: '<span class="me-pin"></span>', iconSize: [22, 22], iconAnchor: [11, 11], popupAnchor: [0, -12] }); }
    };

    if (window.L && L.Marker) L.Marker.prototype.options.icon = window.MEMaps.pinIcon();

    if (window.L && L.Map) {
        L.Map.addInitHook(function () {
            var b = L.latLngBounds(box);
            this.setMaxBounds(b.pad(0.02));
            this.options.maxBoundsViscosity = 1;
            this.setMinZoom(15);
            if (this.attributionControl) this.attributionControl.setPrefix(false);
        });
    }
})();
