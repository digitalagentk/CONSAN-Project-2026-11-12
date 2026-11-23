// =============================================================================
// ME Workshop — visitor tracking
// Every public page sends one small beacon (POST /api/visit) when it loads.
// Visitors who accept cookies get a first-party "me_vid" cookie (1 year) so repeat
// visits are recognised; without consent the visit is still counted, but with no
// cookie and the last part of the IP address blanked. Staff signed in to the
// dashboard are not counted. Log: <data>/visits/yyyy-MM.jsonl (one line per visit).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ConsanWebApp
{
    public class VisitRecord
    {
        public string T { get; set; } = "";       // UTC time, ISO 8601
        public string V { get; set; } = "";       // visitor id (consented visitors only)
        public bool C { get; set; }               // consented to cookies
        public string Path { get; set; } = "";
        public string Ref { get; set; } = "";     // referring site
        public string Ip { get; set; } = "";
        public string Dev { get; set; } = "";     // Mobile | Tablet | Desktop
        public string Os { get; set; } = "";
        public string Br { get; set; } = "";
        public string Lang { get; set; } = "";
        public int W { get; set; }                // screen width
    }

    public static class VisitorFeature
    {
        private static readonly object FileLock = new object();
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions();
        private static readonly Regex Bots = new Regex("bot|crawl|spider|slurp|headless|lighthouse|preview|monitor|curl|wget|python|java/|httpclient", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VidRx = new Regex("^[a-f0-9]{32}$", RegexOptions.Compiled);

        private static string Dir() { return Path.Combine(Store.DataDir, "visits"); }

        public static void MapVisitors(WebApplication app)
        {
            app.MapPost("/api/visit", async (HttpContext ctx) =>
            {
                string ua = ctx.Request.Headers.UserAgent.ToString();
                if (string.IsNullOrWhiteSpace(ua) || Bots.IsMatch(ua)) return Results.NoContent();
                if (AuthExtensions.IsDashboardUser(ctx)) return Results.NoContent(); // staff browsing their own site

                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("path", out var path);
                body.TryGetValue("ref", out var refr);
                body.TryGetValue("lang", out var lang);
                body.TryGetValue("w", out var w);

                path = (path ?? "/").Trim();
                if (!path.StartsWith("/") || path.Length > 200 || path.StartsWith("/api") || path.StartsWith("/admin")) return Results.NoContent();

                bool consent = ctx.Request.Cookies.TryGetValue("me_consent", out var c) && c == "yes";
                string vid = "";
                if (consent)
                {
                    ctx.Request.Cookies.TryGetValue("me_vid", out var existing);
                    vid = existing != null && VidRx.IsMatch(existing) ? existing : Guid.NewGuid().ToString("N");
                    bool https = ctx.Request.IsHttps || string.Equals(ctx.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
                    // Refreshed on every visit so it lasts a year from the last one.
                    ctx.Response.Cookies.Append("me_vid", vid, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        Secure = https,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        IsEssential = true
                    });
                }

                body.TryGetValue("only", out var only);
                if (only == "1") return Results.NoContent(); // cookie issued above; this page was already counted

                string ip = ClientIp(ctx);
                var rec = new VisitRecord
                {
                    T = DateTime.UtcNow.ToString("o"),
                    V = vid,
                    C = consent,
                    Path = path,
                    Ref = RefHost(refr, ctx.Request.Host.Host),
                    Ip = consent ? ip : MaskIp(ip),
                    Dev = Device(ua),
                    Os = Os(ua),
                    Br = Browser(ua),
                    Lang = Array.IndexOf(Pages.Langs, lang) > 0 ? lang : "ar",
                    W = int.TryParse(w, out var wi) ? Math.Clamp(wi, 0, 10000) : 0
                };
                Append(rec);
                return Results.NoContent();
            });

            app.MapGet("/api/admin/visitors", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                int days = 30;
                if (int.TryParse(ctx.Request.Query["days"], out var d)) days = Math.Clamp(d, 1, 365);
                return Results.Json(Report(days));
            });
        }

        private static void Append(VisitRecord rec)
        {
            try
            {
                lock (FileLock)
                {
                    Directory.CreateDirectory(Dir());
                    string file = Path.Combine(Dir(), DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ".jsonl");
                    File.AppendAllText(file, JsonSerializer.Serialize(rec, Json) + "\n");
                }
            }
            catch { /* tracking must never break a page */ }
        }

        private static List<VisitRecord> Load(DateTime fromUtc)
        {
            var list = new List<VisitRecord>();
            if (!Directory.Exists(Dir())) return list;
            var month = new DateTime(fromUtc.Year, fromUtc.Month, 1);
            lock (FileLock)
            {
                for (var m = month; m <= DateTime.UtcNow; m = m.AddMonths(1))
                {
                    string file = Path.Combine(Dir(), m.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ".jsonl");
                    if (!File.Exists(file)) continue;
                    foreach (var line in File.ReadLines(file))
                    {
                        if (line.Length == 0) continue;
                        try
                        {
                            var r = JsonSerializer.Deserialize<VisitRecord>(line, Json);
                            if (r != null && DateTime.Parse(r.T, null, DateTimeStyles.RoundtripKind) >= fromUtc) list.Add(r);
                        }
                        catch { }
                    }
                }
            }
            return list;
        }

        // Visitors without a cookie are told apart by a one-way hash of address + device + day.
        private static string Key(VisitRecord r)
        {
            if (!string.IsNullOrEmpty(r.V)) return r.V;
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(Encoding.UTF8.GetBytes(r.Ip + "|" + r.Os + "|" + r.Br + "|" + r.Dev + "|" + r.T.Substring(0, 10)));
            return "a" + Convert.ToHexString(h).Substring(0, 15).ToLowerInvariant();
        }

        private static DateTime Local(VisitRecord r) { return DateTime.Parse(r.T, null, DateTimeStyles.RoundtripKind).ToUniversalTime().AddHours(3); }

        public static object Report(int days)
        {
            var todayLocal = DateTime.UtcNow.AddHours(3).Date;
            var fromLocal = todayLocal.AddDays(-(days - 1));
            var visits = Load(fromLocal.AddHours(-3));

            Func<DateTime, object> stat = since =>
            {
                var s = visits.Where(v => Local(v) >= since).ToList();
                return new { visits = s.Count, visitors = s.Select(Key).Distinct().Count() };
            };

            var daily = Enumerable.Range(0, days).Select(i =>
            {
                var day = fromLocal.AddDays(i);
                var s = visits.Where(v => Local(v).Date == day).ToList();
                return new { date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), visits = s.Count, visitors = s.Select(Key).Distinct().Count() };
            }).ToList();

            Func<Func<VisitRecord, string>, int, object> top = (sel, n) => visits
                .GroupBy(v => { var k = sel(v); return string.IsNullOrEmpty(k) ? "-" : k; })
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count).Take(n).ToList();

            // Known visitors (accepted cookies): one row each, most recent first.
            var people = visits.Where(v => !string.IsNullOrEmpty(v.V))
                .GroupBy(v => v.V)
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.T).First();
                    return new
                    {
                        id = g.Key.Substring(0, 8).ToUpperInvariant(),
                        firstSeen = Local(g.OrderBy(x => x.T).First()).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        lastSeen = Local(last).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                        visits = g.Count(),
                        days = g.Select(x => x.T.Substring(0, 10)).Distinct().Count(),
                        pages = g.Select(x => x.Path).Distinct().Take(6).ToList(),
                        device = last.Dev + " · " + last.Os + " · " + last.Br,
                        ip = last.Ip,
                        from = g.OrderBy(x => x.T).First().Ref
                    };
                })
                .OrderByDescending(x => x.lastSeen).Take(100).ToList();

            var recent = visits.OrderByDescending(v => v.T).Take(100).Select(v => new
            {
                time = Local(v).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                visitor = string.IsNullOrEmpty(v.V) ? "" : v.V.Substring(0, 8).ToUpperInvariant(),
                path = v.Path,
                from = v.Ref,
                device = v.Dev + " · " + v.Os + " · " + v.Br,
                ip = v.Ip,
                lang = v.Lang
            }).ToList();

            int consented = visits.Count(v => v.C);
            return new
            {
                days,
                today = stat(todayLocal),
                week = stat(todayLocal.AddDays(-6)),
                period = stat(fromLocal),
                consentRate = visits.Count == 0 ? 0 : (int)Math.Round(consented * 100.0 / visits.Count),
                returning = people.Count(p => p.days > 1),
                daily,
                pages = top(v => v.Path, 10),
                referrers = top(v => v.Ref, 10),
                devices = top(v => v.Dev, 5),
                browsers = top(v => v.Br, 8),
                systems = top(v => v.Os, 8),
                people,
                recent
            };
        }

        // ---- helpers ------------------------------------------------------

        private static string ClientIp(HttpContext ctx)
        {
            // Railway's proxy puts the visitor's address first in X-Forwarded-For.
            string fwd = ctx.Request.Headers["X-Forwarded-For"].ToString();
            string ip = !string.IsNullOrWhiteSpace(fwd) ? fwd.Split(',')[0].Trim() : (ctx.Connection.RemoteIpAddress?.ToString() ?? "");
            if (ip.StartsWith("::ffff:")) ip = ip.Substring(7);
            return ip.Length > 45 ? ip.Substring(0, 45) : ip;
        }

        private static string MaskIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return "";
            if (ip.Contains('.'))
            {
                var p = ip.Split('.');
                return p.Length == 4 ? p[0] + "." + p[1] + "." + p[2] + ".x" : "";
            }
            var g = ip.Split(':');
            return string.Join(":", g.Take(3)) + ":x";
        }

        private static string RefHost(string? r, string ownHost)
        {
            if (string.IsNullOrWhiteSpace(r)) return "مباشر";
            if (!Uri.TryCreate(r, UriKind.Absolute, out var u)) return "مباشر";
            string host = u.Host.StartsWith("www.") ? u.Host.Substring(4) : u.Host;
            if (string.Equals(host, ownHost, StringComparison.OrdinalIgnoreCase) || host.EndsWith("kormanyworkshop.com")) return "داخلي";
            if (host.Contains("google.")) return "Google";
            if (host.Contains("facebook.") || host == "fb.com" || host.StartsWith("l.facebook") || host.StartsWith("m.facebook")) return "Facebook";
            if (host.Contains("instagram.")) return "Instagram";
            if (host.Contains("whatsapp.") || host == "wa.me") return "WhatsApp";
            if (host.Contains("t.co") || host.Contains("twitter.") || host.Contains("x.com")) return "X / Twitter";
            if (host.Contains("tiktok.")) return "TikTok";
            if (host.Contains("bing.")) return "Bing";
            return host.Length > 60 ? host.Substring(0, 60) : host;
        }

        private static string Device(string ua)
        {
            if (Regex.IsMatch(ua, "iPad|Tablet", RegexOptions.IgnoreCase) || (ua.Contains("Android") && !ua.Contains("Mobile"))) return "Tablet";
            if (Regex.IsMatch(ua, "Mobile|iPhone|Android", RegexOptions.IgnoreCase)) return "Mobile";
            return "Desktop";
        }

        private static string Os(string ua)
        {
            if (ua.Contains("iPhone") || ua.Contains("iPad")) return "iOS";
            if (ua.Contains("Android")) return "Android";
            if (ua.Contains("Windows")) return "Windows";
            if (ua.Contains("Mac OS X")) return "macOS";
            if (ua.Contains("CrOS")) return "ChromeOS";
            if (ua.Contains("Linux")) return "Linux";
            return "Other";
        }

        private static string Browser(string ua)
        {
            if (ua.Contains("Edg/")) return "Edge";
            if (ua.Contains("OPR/") || ua.Contains("Opera")) return "Opera";
            if (ua.Contains("SamsungBrowser")) return "Samsung";
            if (ua.Contains("FBAN") || ua.Contains("FBAV")) return "Facebook app";
            if (ua.Contains("Instagram")) return "Instagram app";
            if (ua.Contains("Firefox/") || ua.Contains("FxiOS")) return "Firefox";
            if (ua.Contains("CriOS") || ua.Contains("Chrome/")) return "Chrome";
            if (ua.Contains("Safari/")) return "Safari";
            return "Other";
        }
    }
}
