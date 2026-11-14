// =============================================================================
// ME Workshop — Middle Euphrates Workshop · home page
// Copy comes from the site content ([[key]] is filled with the Arabic text on the
// server; data-i18n lets the visitor switch language without a reload).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsanWebApp
{
    public static partial class Pages
    {
        // Approximate centre of the workshop and the box the maps are locked to:
        // Najaf's Industrial District, across the road from the University of Kufa.
        public static readonly double[] WorkshopPin = { 32.0118, 44.3775 };
        public static readonly double[][] DistrictBounds = { new[] { 32.0030, 44.3655 }, new[] { 32.0225, 44.3905 } };

        public static string GetHomePage()
        {
            var content = Store.GetContent();
            var hero = (content.HeroImages ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (hero.Count == 0) hero = Store.DefaultContent().HeroImages;

            var slides = new System.Text.StringBuilder();
            for (int i = 0; i < hero.Count; i++)
            {
                slides.Append("<div class=\"hero-slide").Append(i == 0 ? " active" : "").Append("\"><img src=\"")
                      .Append(System.Net.WebUtility.HtmlEncode(hero[i])).Append("\" alt=\"\"")
                      .Append(i == 0 ? " fetchpriority=\"high\"" : " loading=\"lazy\"").Append(" /></div>");
            }

            var people = new System.Text.StringBuilder();
            foreach (var p in Phones)
            {
                string intl = PhoneIntl(p[1]);
                people.Append("<li class=\"person\"><div><h3 data-i18n=\"").Append(p[0]).Append("\">[[").Append(p[0]).Append("]]</h3>")
                      .Append("<a class=\"num\" href=\"tel:+").Append(intl).Append("\">").Append(PhoneDisplay(p[1])).Append("</a></div>")
                      .Append("<div class=\"person-actions\">")
                      .Append("<a class=\"btn btn-brass\" href=\"tel:+").Append(intl).Append("\">").Append(PhoneSvg.Replace("<svg ", "<svg fill=\"currentColor\" ")).Append("<span data-i18n=\"callLabel\">[[callLabel]]</span></a>")
                      .Append("<a class=\"btn btn-line\" href=\"https://wa.me/").Append(intl).Append("\" target=\"_blank\" rel=\"noopener\">").Append(WhatsAppSvg.Replace("<svg ", "<svg fill=\"currentColor\" ")).Append("<span data-i18n=\"waLabel\">[[waLabel]]</span></a>")
                      .Append("</div></li>");
            }

            var craft = new System.Text.StringBuilder();
            for (int i = 1; i <= 6; i++)
            {
                craft.Append("<li><h3 data-i18n=\"c").Append(i).Append("Title\">[[c").Append(i).Append("Title]]</h3>")
                     .Append("<div><p data-i18n=\"c").Append(i).Append("Desc\">[[c").Append(i).Append("Desc]]</p>")
                     .Append("<small data-i18n=\"c").Append(i).Append("Spec\">[[c").Append(i).Append("Spec]]</small></div></li>");
            }

            var steps = new System.Text.StringBuilder();
            for (int i = 1; i <= 4; i++)
            {
                steps.Append("<li><b data-i18n=\"p").Append(i).Append("Title\">[[p").Append(i).Append("Title]]</b>")
                     .Append("<span data-i18n=\"p").Append(i).Append("Desc\">[[p").Append(i).Append("Desc]]</span></li>");
            }

            string firstIntl = PhoneIntl(Phones[0][1]);
            var galleryItems = Store.GetGallery();

            string body = """
            <body class="has-hero">
                %%NAV%%

                <header class="hero" id="hero">
                    <div class="hero-media" id="heroMedia">%%SLIDES%%</div>
                    <div class="container hero-inner">
                        <div class="hero-copy">
                            <p class="hero-where" data-i18n="heroWhere">[[heroWhere]]</p>
                            <h1 data-i18n="heroTitle">[[heroTitle]]</h1>
                            <span class="hero-en"><b>ME WORKSHOP</b><i>/</i><b>MIDDLE EUPHRATES WORKSHOP</b></span>
                            <span class="hero-alt" lang="ar" dir="rtl">ورشة الفرات الأوسط</span>
                            <p class="hero-lead" data-i18n="heroLead">[[heroLead]]</p>
                            <div class="hero-actions">
                                <a class="btn btn-brass" href="tel:+%%TEL%%">%%PHONEICON%%<span data-i18n="heroCtaCall">[[heroCtaCall]]</span></a>
                                <a class="btn btn-line" href="/service" data-i18n="heroCtaService">[[heroCtaService]]</a>
                            </div>
                        </div>
                        <aside class="plate" aria-label="ME Workshop — Middle Euphrates Workshop, 1945">
                            <span class="plate-rivets" aria-hidden="true"></span>
                            <div class="plate-head">
                                <span class="plate-me">ME WORKSHOP</span>
                                <span class="plate-full">MIDDLE EUPHRATES WORKSHOP</span>
                                <span class="plate-ar">ورشة الفرات الأوسط</span>
                            </div>
                            <dl class="plate-fields">
                                <div class="plate-field wide"><dt data-i18n="plateTypeLabel">[[plateTypeLabel]]</dt><dd data-i18n="plateType">[[plateType]]</dd></div>
                                <div class="plate-field"><dt data-i18n="plateEstLabel">[[plateEstLabel]]</dt><dd class="stamp">1945</dd></div>
                                <div class="plate-field"><dt data-i18n="plateCityLabel">[[plateCityLabel]]</dt><dd data-i18n="plateCity">[[plateCity]]</dd></div>
                                <div class="plate-field wide"><dt data-i18n="plateMgmtLabel">[[plateMgmtLabel]]</dt><dd data-i18n="plateMgmt">[[plateMgmt]]</dd></div>
                            </dl>
                        </aside>
                    </div>
                    <div class="scale" aria-hidden="true">
                        <div class="container scale-inner">
                            <span>1945</span>
                            <span class="scale-bar"><span class="ticks"></span></span>
                            <span data-year-now>[[YEAR_NOW]]</span>
                        </div>
                    </div>
                </header>

                <section class="sec" id="standing">
                    <div class="container">
                        <div class="standing-grid">
                            <h2 data-i18n="standTitle">[[standTitle]]</h2>
                            <p class="lead" data-i18n="standBody">[[standBody]]</p>
                        </div>
                        <div class="facts">
                            <div class="fact"><b>1945</b><span data-i18n="fact1Label">[[fact1Label]]</span></div>
                            <div class="fact"><b data-years>[[YEARS]]</b><span data-i18n="fact2Label">[[fact2Label]]</span></div>
                            <div class="fact"><b data-i18n="fact3Num">[[fact3Num]]</b><span data-i18n="fact3Label">[[fact3Label]]</span></div>
                            <div class="fact"><b data-i18n="fact4Num">[[fact4Num]]</b><span data-i18n="fact4Label">[[fact4Label]]</span></div>
                        </div>
                    </div>
                </section>

                <section class="sec sec-steel" id="story">
                    <span id="about"></span>
                    <div class="container">
                        <div class="story-head">
                            <div>
                                <h2 data-i18n="storyTitle">[[storyTitle]]</h2>
                                <p data-i18n="storySub">[[storySub]]</p>
                            </div>
                            <div class="rail-nav">
                                <button type="button" id="railPrev" data-i18n-label="storyPrev" aria-label="[[storyPrev]]"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M15 5l-7 7 7 7"/></svg></button>
                                <button type="button" id="railNext" data-i18n-label="storyNext" aria-label="[[storyNext]]"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9 5l7 7-7 7"/></svg></button>
                            </div>
                        </div>
                    </div>
                    <div class="rail" id="rail" tabindex="0"></div>
                </section>

                <section class="sec sec-paper" id="craft">
                    <span id="services"></span>
                    <div class="container">
                        <div class="craft-grid">
                            <div class="craft-side">
                                <h2 data-i18n="craftTitle">[[craftTitle]]</h2>
                                <p class="lead" data-i18n="craftSub">[[craftSub]]</p>
                                <div class="craft-photo"><img src="https://images.unsplash.com/photo-1759148413911-5ded7ed2ec06?w=900&h=1125&fit=crop&q=70" alt="" loading="lazy" /></div>
                            </div>
                            <ul class="spec">%%CRAFT%%</ul>
                        </div>
                        <div class="proc">
                            <h3 data-i18n="procTitle">[[procTitle]]</h3>
                            <ol>%%STEPS%%</ol>
                        </div>
                    </div>
                </section>

                %%GALLERY%%

                <section class="sec" id="reach">
                    <div class="container">
                        <div class="reach-grid">
                            <div>
                                <h2 data-i18n="reachTitle">[[reachTitle]]</h2>
                                <p class="lead" data-i18n="reachBody">[[reachBody]]</p>
                                <p class="reach-note" data-i18n="reachNote">[[reachNote]]</p>
                            </div>
                            <div id="iraqMap"></div>
                        </div>
                    </div>
                </section>

                <section class="sec sec-steel" id="contact">
                    <div class="container">
                        <div class="people-grid">
                            <div>
                                <h2 data-i18n="famTitle">[[famTitle]]</h2>
                                <p class="lead" data-i18n="famBody">[[famBody]]</p>
                            </div>
                            <ul class="people">%%PEOPLE%%</ul>
                        </div>
                    </div>
                </section>

                <section class="sec" id="visit">
                    <div class="container">
                        <div class="visit-grid">
                            <div class="visit-info">
                                <h2 data-i18n="visitTitle">[[visitTitle]]</h2>
                                <dl>
                                    <div><dt data-i18n="visitAddrLabel">[[visitAddrLabel]]</dt><dd data-i18n="visitAddr">[[visitAddr]]</dd></div>
                                    <div><dt data-i18n="visitHoursLabel">[[visitHoursLabel]]</dt><dd data-i18n="visitHours">[[visitHours]]</dd></div>
                                </dl>
                                <div class="visit-actions">
                                    <a class="btn btn-brass" id="directionsLink" href="https://www.google.com/maps/search/?api=1&query=%%LAT%%,%%LNG%%" target="_blank" rel="noopener" data-i18n="visitDirections">[[visitDirections]]</a>
                                    <a class="btn btn-line" href="/service" data-i18n="heroCtaService">[[heroCtaService]]</a>
                                </div>
                            </div>
                            <div class="map-frame">
                                <div id="homeMap"></div>
                                <span class="map-caption" data-i18n="visitMapNote">[[visitMapNote]]</span>
                            </div>
                        </div>
                    </div>
                </section>

                %%FOOTER%%
                %%CONTACT%%
                <script defer src="/me/home.js?v=[[V]]"></script>
            </body>
            """;

            body = body.Replace("%%NAV%%", GetNavbarHtml())
                       .Replace("%%SLIDES%%", slides.ToString())
                       .Replace("%%TEL%%", firstIntl)
                       .Replace("%%PHONEICON%%", PhoneSvg.Replace("<svg ", "<svg fill=\"currentColor\" "))
                       .Replace("%%CRAFT%%", craft.ToString())
                       .Replace("%%STEPS%%", steps.ToString())
                       .Replace("%%PEOPLE%%", people.ToString())
                       .Replace("%%LAT%%", WorkshopPin[0].ToString(System.Globalization.CultureInfo.InvariantCulture))
                       .Replace("%%LNG%%", WorkshopPin[1].ToString(System.Globalization.CultureInfo.InvariantCulture))
                       .Replace("%%GALLERY%%", GalleryHomeSection(galleryItems))
                       .Replace("%%FOOTER%%", GetFooterHtml())
                       .Replace("%%CONTACT%%", GetContactWidgetHtml());

            var extra = new Dictionary<string, object>
            {
                ["archive"] = Store.GetArchivalAlbum(),
                ["gallery"] = galleryItems.Take(8).Select(GalleryFeature.PublicView).ToList(),
                ["pin"] = WorkshopPin,
                ["bounds"] = DistrictBounds
            };

            return PageShell("ME Workshop | ورشة الفرات الأوسط — Middle Euphrates Workshop, since 1945", "", body, extra);
        }
    }
}
