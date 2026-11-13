// =============================================================================
// ME Workshop — Middle Euphrates Workshop
// Shared page shell: head, brand mark, header, footer and the contact launcher.
// The look lives in wwwroot/me/site.css and the behaviour in wwwroot/me/site.js.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ConsanWebApp
{
    public static partial class Pages
    {
        // Bump when files under wwwroot/me change so browsers fetch the new ones.
        public const string AssetVersion = "2026-11-13.2";
        public const int FoundedYear = 1945;
        public const string SiteName = "ME Workshop";
        public const string SiteNameFull = "Middle Euphrates Workshop";

        // The workshop's three lines. LabelKey is the content key holding the person's name.
        public static readonly string[][] Phones =
        {
            new[] { "contactLabel1", "07809744298" },
            new[] { "contactLabel2", "07805125702" },
            new[] { "contactLabel3", "07801763759" }
        };

        private static readonly JsonSerializerOptions EmbedJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string PhoneDisplay(string local)
        {
            // 07801763759 -> 0780 176 3759
            if (local == null || local.Length != 11) return local ?? "";
            return local.Substring(0, 4) + " " + local.Substring(4, 3) + " " + local.Substring(7);
        }

        public static string PhoneIntl(string local)
        {
            // 07801763759 -> 9647801763759
            if (string.IsNullOrEmpty(local)) return "";
            return "964" + local.TrimStart('0');
        }

        // Stored copy laid over the built-in defaults, so new keys always have a value.
        public static Dictionary<string, Dictionary<string, string>> Translations()
        {
            var merged = new Dictionary<string, Dictionary<string, string>>();
            var defaults = Store.DefaultContent().Translations;
            var stored = Store.GetContent().Translations ?? new Dictionary<string, Dictionary<string, string>>();
            foreach (var lang in new[] { "ar", "en", "ku" })
            {
                var d = new Dictionary<string, string>();
                if (defaults.TryGetValue(lang, out var def))
                    foreach (var kv in def) d[kv.Key] = kv.Value;
                if (stored.TryGetValue(lang, out var cur) && cur != null)
                    foreach (var kv in cur)
                        if (!string.IsNullOrWhiteSpace(kv.Value)) d[kv.Key] = kv.Value;
                merged[lang] = d;
            }
            return merged;
        }

        private static readonly Regex TokenRegex = new Regex(@"\[\[(\w+)\]\]", RegexOptions.Compiled);

        // Replaces [[key]] with the Arabic copy so the page reads correctly before any script runs.
        public static string Fill(string html, Dictionary<string, string> ar)
        {
            int years = DateTime.UtcNow.Year - FoundedYear;
            return TokenRegex.Replace(html, m =>
            {
                string key = m.Groups[1].Value;
                if (key == "YEARS") return years.ToString();
                if (key == "YEAR_NOW") return DateTime.UtcNow.Year.ToString();
                if (key == "V") return AssetVersion;
                return ar.TryGetValue(key, out var v) ? v.Replace("{n}", years.ToString()) : "";
            });
        }

        public static string PageShell(string title, string css, string body)
        {
            return PageShell(title, css, body, null);
        }

        public static string PageShell(string title, string css, string body, Dictionary<string, object>? extra)
        {
            var t = Translations();
            var ar = t["ar"];

            var embed = new Dictionary<string, object>
            {
                ["t"] = t,
                ["founded"] = FoundedYear,
                ["phones"] = Phones.Select(p => new Dictionary<string, string>
                {
                    ["key"] = p[0],
                    ["local"] = PhoneDisplay(p[1]),
                    ["intl"] = PhoneIntl(p[1])
                }).ToList()
            };
            if (extra != null)
                foreach (var kv in extra) embed[kv.Key] = kv.Value;

            string json = JsonSerializer.Serialize(embed, EmbedJson);
            string description = StripTags(ar.TryGetValue("footerText", out var ft) ? ft : "");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"ar\" dir=\"rtl\" data-theme=\"dark\">\n<head>\n");
            sb.Append("<meta charset=\"utf-8\" />\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n");
            sb.Append("<title>").Append(Encode(title)).Append("</title>\n");
            sb.Append("<meta name=\"description\" content=\"").Append(Encode(description)).Append("\" />\n");
            sb.Append("<meta name=\"theme-color\" content=\"#121517\" />\n");
            sb.Append("<meta property=\"og:type\" content=\"website\" />\n");
            sb.Append("<meta property=\"og:site_name\" content=\"ME Workshop — Middle Euphrates Workshop\" />\n");
            sb.Append("<meta property=\"og:title\" content=\"").Append(Encode(title)).Append("\" />\n");
            sb.Append("<meta property=\"og:description\" content=\"").Append(Encode(description)).Append("\" />\n");
            sb.Append("<meta property=\"og:image\" content=\"/me/share.png\" />\n");
            sb.Append("<meta property=\"og:locale\" content=\"ar_IQ\" />\n");
            sb.Append("<meta name=\"twitter:card\" content=\"summary_large_image\" />\n");
            sb.Append("<link rel=\"icon\" href=\"/me/icon.svg?v=").Append(AssetVersion).Append("\" type=\"image/svg+xml\" />\n");
            sb.Append("<link rel=\"preload\" href=\"/me/fonts/kufi-ar.woff2\" as=\"font\" type=\"font/woff2\" crossorigin />\n");
            sb.Append("<link rel=\"preload\" href=\"/me/fonts/sans-ar.woff2\" as=\"font\" type=\"font/woff2\" crossorigin />\n");
            sb.Append("<script>(function(){var d=document.documentElement;d.classList.add('js');try{var t=localStorage.getItem('me-theme');if(t==='light')d.setAttribute('data-theme','light');var l=localStorage.getItem('me-lang');if(l==='en'||l==='ku'){d.lang=l;d.dir=l==='en'?'ltr':'rtl';d.classList.add('me-pending');setTimeout(function(){d.classList.remove('me-pending')},1200);}}catch(e){}})();</script>\n");
            sb.Append("<link rel=\"stylesheet\" href=\"/me/site.css?v=").Append(AssetVersion).Append("\" />\n");
            if (!string.IsNullOrWhiteSpace(css))
                sb.Append("<style>").Append(css).Append("</style>\n");
            sb.Append("<link rel=\"stylesheet\" href=\"/me/inner.css?v=").Append(AssetVersion).Append("\" />\n");
            sb.Append("<script>window.ME_CONTENT=").Append(json).Append(";</script>\n");
            sb.Append("<script defer src=\"/me/site.js?v=").Append(AssetVersion).Append("\"></script>\n");
            sb.Append("</head>\n");
            sb.Append(Fill(body, ar));
            sb.Append("\n</html>");
            return sb.ToString();
        }

        private static string Encode(string s)
        {
            return System.Net.WebUtility.HtmlEncode(s ?? "");
        }

        private static string StripTags(string s)
        {
            return Regex.Replace(s ?? "", "<[^>]+>", "").Trim();
        }

        // The inner pages still ask for these. The styles now come from /me/site.css
        // and the behaviour from /me/site.js, so there is nothing to inline.
        public static string GetSharedStyles() { return ""; }
        public static string GetCoreNavScript() { return ""; }
        public static string GetContactWidgetScript() { return ""; }

        public static string GetBrandSvg()
        {
            return """
            <svg class="brand-icon" viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                <defs>
                    <linearGradient id="meBrass" x1="0" y1="0" x2="1" y2="1">
                        <stop offset="0" stop-color="#E6CD9E"/><stop offset="0.45" stop-color="#B8935A"/><stop offset="0.62" stop-color="#E3CB98"/><stop offset="1" stop-color="#A9844C"/>
                    </linearGradient>
                </defs>
                <rect x="1.5" y="6.5" width="45" height="35" rx="4" fill="url(#meBrass)" stroke="#5E4620" stroke-width="1"/>
                <g fill="#5E4620"><circle cx="6" cy="11" r="1.3"/><circle cx="42" cy="11" r="1.3"/><circle cx="6" cy="37" r="1.3"/><circle cx="42" cy="37" r="1.3"/></g>
                <g fill="none" stroke="#17130B" stroke-width="3.4" stroke-linecap="square" stroke-linejoin="miter">
                    <path d="M11 31.5V16.5l6.5 9 6.5-9v15"/>
                    <path d="M37.5 16.5H30v15h7.5M30 24h6"/>
                </g>
            </svg>
            """;
        }

        public static string GetNavbarHtml()
        {
            return """
            <nav class="navbar" id="navbar">
                <div class="container nav-container">
                    <a href="/" class="brand-link" aria-label="ME Workshop — Middle Euphrates Workshop">
                        %%BRAND%%
                        <span class="brand-text">
                            <span class="brand-name">ME WORKSHOP</span>
                            <span class="brand-tagline" data-i18n="brandFull">[[brandFull]]</span>
                        </span>
                    </a>
                    <ul class="nav-menu" id="navMenu">
                        <li><a class="nav-link" href="/" data-i18n="navHome">[[navHome]]</a></li>
                        <li><a class="nav-link" href="/#story" data-i18n="navAbout">[[navAbout]]</a></li>
                        <li><a class="nav-link" href="/#craft" data-i18n="navServices">[[navServices]]</a></li>
                        <li><a class="nav-link" href="/industrial-services" data-i18n="navIndustrial">[[navIndustrial]]</a></li>
                        <li><a class="nav-link" href="/explorer" data-i18n="navExplorer">[[navExplorer]]</a></li>
                        <li><a class="nav-link" href="/guide" data-i18n="navGuide">[[navGuide]]</a></li>
                        <li>
                            <div class="nav-controls">
                                <div class="lang-switch" role="group" aria-label="Language">
                                    <button type="button" data-lang="ar" aria-pressed="true" onclick="switchLanguage('ar')">عربي</button>
                                    <button type="button" data-lang="en" aria-pressed="false" onclick="switchLanguage('en')">EN</button>
                                    <button type="button" data-lang="ku" aria-pressed="false" onclick="switchLanguage('ku')">کوردی</button>
                                </div>
                                <button type="button" class="theme-toggle" onclick="toggleTheme()" aria-label="Light / dark">
                                    <svg class="i-moon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z"/></svg>
                                    <svg class="i-sun" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>
                                </button>
                                <a class="btn btn-brass nav-cta" href="/service" data-i18n="navCta">[[navCta]]</a>
                            </div>
                        </li>
                    </ul>
                    <button type="button" class="nav-toggle" onclick="toggleMenu()" aria-label="Menu" aria-expanded="false" aria-controls="navMenu">
                        <span></span><span></span><span></span>
                    </button>
                </div>
                <!-- Targets the inner pages' own scripts still write to. Kept out of sight. -->
                <div id="legacySink" hidden>
                    <span id="navHome"></span><span id="navAbout"></span><span id="navServices"></span><span id="navIndustrial"></span><span id="navExplorer"></span><span id="navGuide"></span>
                    <span id="langOptAr"></span><span id="langOptEn"></span><span id="langOptKu"></span><span id="langDropdown"></span><span id="globeBtn"></span>
                    <span id="lightIcon"></span><span id="darkIcon"></span>
                    <span id="footerText"></span><span id="footerCopy"></span><span id="footerLinkedin"></span><span id="footerFacebook"></span><span id="footerInstagram"></span>
                    <span id="contactTitle"></span><span id="contactLabel1"></span><span id="contactLabel2"></span><span id="contactLabel3"></span><span id="contactBadgeText"></span><span id="contactPopup"></span>
                </div>
            </nav>
            """.Replace("%%BRAND%%", GetBrandSvg());
        }

        public static string GetFooterHtml()
        {
            var phones = new StringBuilder();
            foreach (var p in Phones)
            {
                phones.Append("<li><span data-i18n=\"").Append(p[0]).Append("\">[[").Append(p[0]).Append("]]</span>")
                      .Append("<a href=\"tel:+").Append(PhoneIntl(p[1])).Append("\"><bdi>").Append(PhoneDisplay(p[1])).Append("</bdi></a></li>");
            }

            return """
            <footer class="footer">
                <div class="container">
                    <div class="footer-grid">
                        <div class="footer-about">
                            <div class="footer-logo">
                                %%BRAND%%
                                <span class="footer-brand-text">
                                    <span class="footer-brand-name">ME WORKSHOP</span>
                                    <span class="footer-brand-tagline">Middle Euphrates Workshop</span>
                                </span>
                            </div>
                            <p data-i18n="footerText">[[footerText]]</p>
                        </div>
                        <div>
                            <h4 data-i18n="footerLinksTitle">[[footerLinksTitle]]</h4>
                            <ul class="footer-list">
                                <li><a href="/#story" data-i18n="navAbout">[[navAbout]]</a></li>
                                <li><a href="/#craft" data-i18n="navServices">[[navServices]]</a></li>
                                <li><a href="/industrial-services" data-i18n="navIndustrial">[[navIndustrial]]</a></li>
                                <li><a href="/explorer" data-i18n="navExplorer">[[navExplorer]]</a></li>
                                <li><a href="/guide" data-i18n="navGuide">[[navGuide]]</a></li>
                                <li><a href="/service" data-i18n="navCta">[[navCta]]</a></li>
                                <li><a href="/login" data-i18n="navLogin">[[navLogin]]</a></li>
                            </ul>
                        </div>
                        <div>
                            <h4 data-i18n="footerCallTitle">[[footerCallTitle]]</h4>
                            <ul class="footer-list footer-phones">%%PHONES%%</ul>
                        </div>
                    </div>
                    <div class="footer-bottom">
                        <span>© <span data-year-now>[[YEAR_NOW]]</span> <span data-i18n="footerCopy">[[footerCopy]]</span></span>
                        <span class="mock-note" data-i18n="footerMock">[[footerMock]]</span>
                    </div>
                </div>
            </footer>
            """.Replace("%%BRAND%%", GetBrandSvg().Replace("brand-icon", "footer-brand-icon").Replace("meBrass", "meBrassF"))
               .Replace("%%PHONES%%", phones.ToString());
        }

        private const string PhoneSvg = "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M6.6 10.8a15.1 15.1 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.25c1.1.37 2.3.57 3.6.57a1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1c0 1.25.2 2.45.57 3.57a1 1 0 0 1-.25 1L6.6 10.8z\"/></svg>";
        private const string WhatsAppSvg = "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.3A10 10 0 1 0 12 2zm0 18.2a8.2 8.2 0 0 1-4.2-1.15l-.3-.18-3 .78.8-2.9-.2-.3A8.2 8.2 0 1 1 12 20.2zm4.5-6.1c-.25-.12-1.45-.72-1.68-.8-.22-.08-.39-.12-.55.12-.16.25-.63.8-.78.97-.14.16-.29.18-.53.06a6.7 6.7 0 0 1-3.3-2.9c-.25-.43.25-.4.72-1.33.08-.16.04-.3-.02-.43-.06-.12-.55-1.33-.76-1.82-.2-.48-.4-.41-.55-.42h-.47c-.16 0-.43.06-.65.3-.22.25-.86.84-.86 2.05 0 1.2.88 2.37 1 2.53.12.16 1.73 2.64 4.2 3.7 1.56.67 2.17.73 2.95.61.47-.07 1.45-.6 1.66-1.17.2-.57.2-1.06.14-1.17-.06-.1-.22-.16-.47-.28z\"/></svg>";

        public static string GetContactWidgetHtml()
        {
            var rows = new StringBuilder();
            foreach (var p in Phones)
            {
                string intl = PhoneIntl(p[1]);
                rows.Append("<li class=\"phone-row\"><span class=\"who\"><span data-i18n=\"").Append(p[0]).Append("\">[[").Append(p[0]).Append("]]</span>")
                    .Append("<bdi class=\"num\">").Append(PhoneDisplay(p[1])).Append("</bdi></span>")
                    .Append("<a class=\"icon-btn\" href=\"tel:+").Append(intl).Append("\" data-i18n-label=\"callLabel\" aria-label=\"[[callLabel]]\">").Append(PhoneSvg).Append("</a>")
                    .Append("<a class=\"icon-btn\" href=\"https://wa.me/").Append(intl).Append("\" target=\"_blank\" rel=\"noopener\" data-i18n-label=\"waLabel\" aria-label=\"[[waLabel]]\">").Append(WhatsAppSvg).Append("</a></li>");
            }

            return """
            <div class="floating-contact" id="floatingContact">
                <div class="contact-sheet" id="contactSheet" role="dialog" aria-labelledby="contactSheetTitle">
                    <div class="contact-sheet-head">
                        <strong id="contactSheetTitle" data-i18n="contactTitle">[[contactTitle]]</strong>
                        <button type="button" class="close-btn" onclick="toggleContact(false)" aria-label="Close"><svg viewBox="0 0 24 24" stroke-linecap="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>
                    </div>
                    <ul class="phone-rows">%%ROWS%%</ul>
                </div>
                <button type="button" class="btn btn-brass contact-badge" onclick="toggleContact()" aria-expanded="false" aria-controls="contactSheet">
                    %%PHONE%%<span data-i18n="contactBadgeText">[[contactBadgeText]]</span>
                </button>
            </div>
            """.Replace("%%ROWS%%", rows.ToString()).Replace("%%PHONE%%", PhoneSvg.Replace("<svg ", "<svg fill=\"currentColor\" "));
        }
    }
}
