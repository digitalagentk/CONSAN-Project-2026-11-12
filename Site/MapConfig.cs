// =============================================================================
// ME Workshop — one source of truth for every map on the site.
// The workshop's position is the "workshop" pin managed in the dashboard
// (Map & locations tab). The home page, the industrial-services page and the
// dashboard all read it from here, so moving the pin moves every map.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace ConsanWebApp
{
    public static partial class Store
    {
        /// <summary>The workshop's current position (the editable workshop pin), or the built-in default.</summary>
        public static double[] WorkshopLocation()
        {
            var ws = GetApprovedMapPins().FirstOrDefault(p => p.IsWorkshop);
            if (ws == null || (ws.Latitude == 0 && ws.Longitude == 0)) return Pages.WorkshopPin;
            return new[] { ws.Latitude, ws.Longitude };
        }
    }

    public static partial class Pages
    {
        /// <summary>
        /// Map settings shared by every page: the workshop position and the area visitors can pan in.
        /// The area is the Industrial District, grown to include every approved location, so a pin
        /// placed outside the district is never cut off.
        /// </summary>
        public static Dictionary<string, object> MapConfig()
        {
            var pin = Store.WorkshopLocation();
            double minLat = DistrictBounds[0][0], minLng = DistrictBounds[0][1];
            double maxLat = DistrictBounds[1][0], maxLng = DistrictBounds[1][1];
            foreach (var p in Store.GetApprovedMapPins())
            {
                minLat = Math.Min(minLat, p.Latitude); maxLat = Math.Max(maxLat, p.Latitude);
                minLng = Math.Min(minLng, p.Longitude); maxLng = Math.Max(maxLng, p.Longitude);
            }
            const double pad = 0.003; // ~300 m around the outermost pin
            return new Dictionary<string, object>
            {
                ["pin"] = pin,
                ["bounds"] = new[] { new[] { minLat - pad, minLng - pad }, new[] { maxLat + pad, maxLng + pad } }
            };
        }

        /// <summary>Inline script for pages that don't go through PageShell (the dashboard).</summary>
        public static string MapConfigScript()
        {
            return "<script>window.ME_MAP=" + JsonSerializer.Serialize(MapConfig()) + ";</script>";
        }

        public static string Coord(double v) { return v.ToString("0.######", CultureInfo.InvariantCulture); }
    }
}
