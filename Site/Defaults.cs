// =============================================================================
// ME Workshop — Middle Euphrates Workshop · built-in content
// Everything here can be edited from the admin dashboard once the site is running.
// A "*" marks placeholder details that should be replaced with the real ones.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsanWebApp
{
    public static partial class Store
    {
        internal static SiteContentData DefaultContent()
        {
            var data = new SiteContentData();
            data.HeroImages = new List<string>
            {
                "https://images.unsplash.com/photo-1554231063-fef7ed86c0b7?w=1920&h=1200&fit=crop&q=70",
                "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?w=1920&h=1200&fit=crop&q=70",
                "https://images.unsplash.com/photo-1759148413911-5ded7ed2ec06?w=1920&h=1200&fit=crop&q=70",
                "https://images.unsplash.com/photo-1579107821380-a2f5df32d67f?w=1920&h=1200&fit=crop&q=70"
            };

            data.Translations["ar"] = new Dictionary<string, string>
            {
                ["navHome"] = "الرئيسية",
                ["navAbout"] = "تاريخ الورشة",
                ["navServices"] = "ما نصلّحه",
                ["navIndustrial"] = "خدمات صناعية",
                ["navExplorer"] = "المستكشف",
                ["navGuide"] = "الدليل الشامل",
                ["navCta"] = "اطلب خدمة",
                ["navLogin"] = "دخول الإدارة",
                ["brandFull"] = "ورشة الفرات الأوسط",
                ["footerText"] = "ورشة الفرات الأوسط لتصليح وبيع بمبات ونوزلات الديزل. أول ورشة في العراق لتصليح مضخات حقن الوقود، في النجف الأشرف منذ 1945.",
                ["footerLinksTitle"] = "الموقع",
                ["footerCallTitle"] = "اتصل بنا",
                ["footerCopy"] = "ورشة الفرات الأوسط. جميع الحقوق محفوظة.",
                ["footerMock"] = "* بيانات تجريبية ستُستبدل بالمعلومات الحقيقية للورشة.",
                ["contactBadgeText"] = "اتصل بالورشة",
                ["contactTitle"] = "أرقام الورشة",
                ["contactLabel1"] = "ابو مصطفى",
                ["contactLabel2"] = "ابو رضا",
                ["contactLabel3"] = "ابو علي",
                ["callLabel"] = "اتصال",
                ["waLabel"] = "واتساب",
                ["heroWhere"] = "النجف الأشرف، الحي الصناعي",
                ["heroTitle"] = "ورشة الفرات الأوسط",
                ["heroLead"] = "أول ورشة في العراق لتصليح مضخات حقن الوقود. منذ عام 1945 ونحن نصلّح ونعاير ونبيع بمبات ونوزلات الديزل لأصحاب المحركات في أنحاء العراق.",
                ["heroCtaCall"] = "اتصل بالورشة",
                ["heroCtaService"] = "اطلب خدمة",
                ["plateTypeLabel"] = "الاختصاص",
                ["plateType"] = "بمبات ونوزلات الديزل",
                ["plateEstLabel"] = "سنة التأسيس",
                ["plateCityLabel"] = "الموقع",
                ["plateCity"] = "النجف، العراق",
                ["plateMgmtLabel"] = "الإدارة",
                ["plateMgmt"] = "الحاج سمير محمد الكرماني وأولاده",
                ["scaleYears"] = "{n} سنة من العمل المتواصل",
                ["standTitle"] = "اسمٌ يعرفه أصحاب المحركات في كل العراق",
                ["standBody"] = "عام 1945 لم يكن في العراق ورشة تصلّح مضخة حقن الوقود. فتحت هذه الورشة أبوابها في النجف وصارت الأولى. ومنذ ذلك اليوم يقصدها سائقو الشاحنات وأصحاب المزارع والمولدات من البصرة إلى الموصل، ويعودون إليها جيلاً بعد جيل.",
                ["fact1Label"] = "سنة التأسيس في النجف الأشرف",
                ["fact2Label"] = "سنة من العمل المتواصل",
                ["fact3Num"] = "3*",
                ["fact3Label"] = "أجيال من العائلة نفسها",
                ["fact4Num"] = "18*",
                ["fact4Label"] = "محافظة يقصدنا منها الزبائن",
                ["storyTitle"] = "من 1945 إلى اليوم",
                ["storySub"] = "محطات من سجلّ الورشة، من أول طاولة عمل إلى طاولات الفحص الحديثة.",
                ["storyPrev"] = "السابق",
                ["storyNext"] = "التالي",
                ["craftTitle"] = "اختصاص واحد منذ البداية",
                ["craftSub"] = "نظام حقن الوقود في محرك الديزل: المضخة، والنوزل، وكل ما بينهما.",
                ["c1Title"] = "مضخات الحقن الخطية",
                ["c1Desc"] = "تفكيك كامل، تبديل البلانجرات وصمامات الضخ، وضبط كمية الوقود والتوقيت لكل أسطوانة.",
                ["c1Spec"] = "شاحنات، ساحبات زراعية، مولدات",
                ["c2Title"] = "المضخات الدوّارة",
                ["c2Desc"] = "صيانة مضخات التوزيع الدوّارة، وتبديل رأس التوزيع ومنظّم السرعة والحشوات.",
                ["c2Spec"] = "VE و DPA *",
                ["c3Title"] = "النوزلات والحاقنات",
                ["c3Desc"] = "فحص ضغط الفتح وشكل الرذاذ والتسريب، وتبديل الإبر والرؤوس وإعادة الضبط.",
                ["c3Spec"] = "ميكانيكية وإلكترونية *",
                ["c4Title"] = "أنظمة الكومن ريل *",
                ["c4Desc"] = "فحص مضخات الضغط العالي والحاقنات الإلكترونية وصيانتها. *",
                ["c4Spec"] = "Bosch، Denso، Delphi *",
                ["c5Title"] = "الفحص والمعايرة",
                ["c5Desc"] = "كل مضخة تُشغَّل على طاولة الفحص وتُعاير على أرقام الشركة المصنّعة قبل أن تغادر الورشة.",
                ["c5Spec"] = "طاولة فحص لكل نوع *",
                ["c6Title"] = "البيع وقطع الغيار",
                ["c6Desc"] = "بمبات ونوزلات جديدة ومجدَّدة، وقطع غيار لمعظم الأنواع المتداولة في العراق.",
                ["c6Spec"] = "ضمان على العمل *",
                ["procTitle"] = "كيف تمرّ البمبة في الورشة",
                ["p1Title"] = "الاستلام والتشخيص",
                ["p1Desc"] = "نسمع المشكلة ونفحص البمبة قبل فتحها.",
                ["p2Title"] = "التفكيك والتبديل",
                ["p2Desc"] = "تُفكّك كاملة وتُبدَّل القطع المستهلكة.",
                ["p3Title"] = "المعايرة على الطاولة",
                ["p3Desc"] = "تُضبط الكمية والتوقيت على طاولة الفحص.",
                ["p4Title"] = "التسليم",
                ["p4Desc"] = "تُسلَّم جاهزة للتركيب مع ضمان على العمل. *",
                ["reachTitle"] = "من كل المحافظات إلى النجف",
                ["reachBody"] = "تصلنا البمبات والنوزلات من ورش وسائقين ومزارعين في أنحاء البلاد، وتعود إليهم بعد الفحص والمعايرة.",
                ["reachNote"] = "* توزيع المحافظات على الخريطة توضيحي بانتظار بيانات الورشة.",
                ["riverEuphrates"] = "الفرات",
                ["riverTigris"] = "دجلة",
                ["famTitle"] = "تكلّم مع أصحاب الورشة مباشرة",
                ["famBody"] = "الورشة بإدارة الحاج سمير محمد الكرماني وأولاده. من يردّ على اتصالك هو من يعمل على بمبتك.",
                ["visitTitle"] = "زُر الورشة",
                ["visitAddrLabel"] = "العنوان",
                ["visitAddr"] = "النجف الأشرف، الحي الصناعي، منتصف شارع إعدادية الصناعة، مقابل جامعة الكوفة",
                ["visitHoursLabel"] = "ساعات العمل",
                ["visitHours"] = "من السبت إلى الخميس، 8 صباحاً حتى 6 مساءً. الجمعة عطلة. *",
                ["visitDirections"] = "افتح في خرائط Google",
                ["visitMapNote"] = "موقع العلامة تقريبي *",
                ["mapPinTitle"] = "ورشة الفرات الأوسط"
            };

            data.Translations["en"] = new Dictionary<string, string>
            {
                ["navHome"] = "Home",
                ["navAbout"] = "Our history",
                ["navServices"] = "What we repair",
                ["navIndustrial"] = "Industrial services",
                ["navExplorer"] = "Explorer",
                ["navGuide"] = "Guide",
                ["navCta"] = "Request service",
                ["navLogin"] = "Staff login",
                ["brandFull"] = "Middle Euphrates Workshop",
                ["footerText"] = "Middle Euphrates Workshop repairs and sells diesel injection pumps and nozzles. The first workshop in Iraq to repair fuel injection pumps, in Najaf since 1945.",
                ["footerLinksTitle"] = "Site",
                ["footerCallTitle"] = "Call us",
                ["footerCopy"] = "Middle Euphrates Workshop. All rights reserved.",
                ["footerMock"] = "* Placeholder details, to be replaced with the workshop's real information.",
                ["contactBadgeText"] = "Call the workshop",
                ["contactTitle"] = "Workshop numbers",
                ["contactLabel1"] = "Abu Mustafa",
                ["contactLabel2"] = "Abu Ridha",
                ["contactLabel3"] = "Abu Ali",
                ["callLabel"] = "Call",
                ["waLabel"] = "WhatsApp",
                ["heroWhere"] = "Najaf, the Industrial District",
                ["heroTitle"] = "Middle Euphrates Workshop",
                ["heroLead"] = "The first workshop in Iraq to repair fuel injection pumps. Since 1945 we have repaired, calibrated and sold diesel pumps and nozzles for engine owners across Iraq.",
                ["heroCtaCall"] = "Call the workshop",
                ["heroCtaService"] = "Request service",
                ["plateTypeLabel"] = "Speciality",
                ["plateType"] = "Diesel pumps and nozzles",
                ["plateEstLabel"] = "Established",
                ["plateCityLabel"] = "Location",
                ["plateCity"] = "Najaf, Iraq",
                ["plateMgmtLabel"] = "Management",
                ["plateMgmt"] = "Hajj Samir Mohammed Al-Kermani and sons",
                ["scaleYears"] = "{n} years of continuous work",
                ["standTitle"] = "A name engine owners know across Iraq",
                ["standBody"] = "In 1945 no workshop in Iraq repaired fuel injection pumps. This one opened in Najaf and became the first. Truck drivers, farmers and generator owners have brought their pumps here ever since, from Basra to Mosul, one generation after another.",
                ["fact1Label"] = "Founded in Najaf",
                ["fact2Label"] = "years of continuous work",
                ["fact3Num"] = "3*",
                ["fact3Label"] = "generations of the same family",
                ["fact4Num"] = "18*",
                ["fact4Label"] = "governorates our customers come from",
                ["storyTitle"] = "From 1945 to today",
                ["storySub"] = "Moments from the workshop's record, from the first workbench to today's test benches.",
                ["storyPrev"] = "Previous",
                ["storyNext"] = "Next",
                ["craftTitle"] = "One speciality from the start",
                ["craftSub"] = "The fuel injection system of the diesel engine: the pump, the nozzle, and everything between them.",
                ["c1Title"] = "Inline injection pumps",
                ["c1Desc"] = "Full strip-down, new plungers and delivery valves, then fuel quantity and timing set cylinder by cylinder.",
                ["c1Spec"] = "Trucks, tractors, generators",
                ["c2Title"] = "Rotary pumps",
                ["c2Desc"] = "Distributor-type pumps overhauled, with head, governor and seals replaced as needed.",
                ["c2Spec"] = "VE and DPA *",
                ["c3Title"] = "Nozzles and injectors",
                ["c3Desc"] = "Opening pressure, spray pattern and leak-back tested; needles and tips replaced and reset.",
                ["c3Spec"] = "Mechanical and electronic *",
                ["c4Title"] = "Common rail systems *",
                ["c4Desc"] = "High-pressure pumps and electronic injectors tested and repaired. *",
                ["c4Spec"] = "Bosch, Denso, Delphi *",
                ["c5Title"] = "Testing and calibration",
                ["c5Desc"] = "Every pump is run on the test bench and calibrated to the maker's figures before it leaves.",
                ["c5Spec"] = "A bench for each pump type *",
                ["c6Title"] = "Sales and spare parts",
                ["c6Desc"] = "New and reconditioned pumps and nozzles, and parts for most types in use in Iraq.",
                ["c6Spec"] = "Work under warranty *",
                ["procTitle"] = "How a pump goes through the workshop",
                ["p1Title"] = "Intake and diagnosis",
                ["p1Desc"] = "We hear the fault and test the pump before opening it.",
                ["p2Title"] = "Strip and replace",
                ["p2Desc"] = "Fully dismantled; worn parts are replaced.",
                ["p3Title"] = "Bench calibration",
                ["p3Desc"] = "Quantity and timing are set on the test bench.",
                ["p4Title"] = "Handover",
                ["p4Desc"] = "Returned ready to fit, with a warranty on the work. *",
                ["reachTitle"] = "From every governorate to Najaf",
                ["reachBody"] = "Pumps and nozzles reach us from garages, drivers and farmers around the country, and go back to them tested and calibrated.",
                ["reachNote"] = "* The governorates shown are illustrative until the workshop's own figures are added.",
                ["riverEuphrates"] = "Euphrates",
                ["riverTigris"] = "Tigris",
                ["famTitle"] = "Speak to the owners directly",
                ["famBody"] = "The workshop is run by Hajj Samir Mohammed Al-Kermani and his sons. Whoever answers your call is the one who works on your pump.",
                ["visitTitle"] = "Visit the workshop",
                ["visitAddrLabel"] = "Address",
                ["visitAddr"] = "Najaf, Industrial District, midway along Industrial High School Street, opposite the University of Kufa",
                ["visitHoursLabel"] = "Opening hours",
                ["visitHours"] = "Saturday to Thursday, 8 am to 6 pm. Closed on Friday. *",
                ["visitDirections"] = "Open in Google Maps",
                ["visitMapNote"] = "Pin position is approximate *",
                ["mapPinTitle"] = "Middle Euphrates Workshop"
            };

            data.Translations["ku"] = new Dictionary<string, string>
            {
                ["navHome"] = "سەرەکی",
                ["navAbout"] = "مێژووی کارگە",
                ["navServices"] = "چی چاک دەکەینەوە",
                ["navIndustrial"] = "خزمەتی پیشەسازی",
                ["navExplorer"] = "گەڕان",
                ["navGuide"] = "ڕێنمایی گشتی",
                ["navCta"] = "داوای خزمەت بکە",
                ["navLogin"] = "چوونەژوورەوەی ستاف",
                ["brandFull"] = "کارگەی فوراتی ناوەڕاست",
                ["footerText"] = "کارگەی فوراتی ناوەڕاست بۆ چاککردنەوە و فرۆشتنی پەمپ و نۆزلی دیزڵ. یەکەم کارگە لە عێراق بۆ چاککردنەوەی پەمپی سووتەمەنی، لە نەجەفی ئەشرەف لە ساڵی 1945ـەوە.",
                ["footerLinksTitle"] = "ماڵپەڕ",
                ["footerCallTitle"] = "پەیوەندیمان پێوە بکە",
                ["footerCopy"] = "کارگەی فوراتی ناوەڕاست. هەموو مافەکان پارێزراون.",
                ["footerMock"] = "* زانیاریی نموونەیی، بە زانیاریی ڕاستەقینەی کارگە دەگۆڕدرێت.",
                ["contactBadgeText"] = "پەیوەندی بە کارگەوە بکە",
                ["contactTitle"] = "ژمارەکانی کارگە",
                ["contactLabel1"] = "ئەبو مستەفا",
                ["contactLabel2"] = "ئەبو ڕەزا",
                ["contactLabel3"] = "ئەبو عەلی",
                ["callLabel"] = "پەیوەندی",
                ["waLabel"] = "واتساپ",
                ["heroWhere"] = "نەجەفی ئەشرەف، گەڕەکی پیشەسازی",
                ["heroTitle"] = "کارگەی فوراتی ناوەڕاست",
                ["heroLead"] = "یەکەم کارگە لە عێراق بۆ چاککردنەوەی پەمپی سووتەمەنی. لە ساڵی 1945ـەوە پەمپ و نۆزلی دیزڵ چاک دەکەینەوە و ڕێکیان دەخەین و دەیانفرۆشین بۆ خاوەن بزوێنەرەکان لە سەرانسەری عێراق.",
                ["heroCtaCall"] = "پەیوەندی بە کارگەوە بکە",
                ["heroCtaService"] = "داوای خزمەت بکە",
                ["plateTypeLabel"] = "پسپۆڕی",
                ["plateType"] = "پەمپ و نۆزلی دیزڵ",
                ["plateEstLabel"] = "ساڵی دامەزراندن",
                ["plateCityLabel"] = "شوێن",
                ["plateCity"] = "نەجەف، عێراق",
                ["plateMgmtLabel"] = "بەڕێوەبەرایەتی",
                ["plateMgmt"] = "حاجی سەمیر محەمەد کەرمانی و کوڕەکانی",
                ["scaleYears"] = "{n} ساڵ کاری بەردەوام",
                ["standTitle"] = "ناوێک کە خاوەن بزوێنەرەکان لە هەموو عێراق دەیناسن",
                ["standBody"] = "ساڵی 1945 هیچ کارگەیەک لە عێراق پەمپی سووتەمەنی چاک نەدەکردەوە. ئەم کارگەیە لە نەجەف کرایەوە و بوو بە یەکەم. لەو ڕۆژەوە شۆفێری بارهەڵگر و جووتیار و خاوەن مۆلیدەکان لە بەسرەوە تا مووسڵ ڕووی تێدەکەن، نەوە دوای نەوە.",
                ["fact1Label"] = "ساڵی دامەزراندن لە نەجەف",
                ["fact2Label"] = "ساڵ کاری بەردەوام",
                ["fact3Num"] = "3*",
                ["fact3Label"] = "نەوە لە هەمان خێزان",
                ["fact4Num"] = "18*",
                ["fact4Label"] = "پارێزگا کە کڕیارەکانمان لێوەی دێن",
                ["storyTitle"] = "لە 1945ـەوە تا ئەمڕۆ",
                ["storySub"] = "چەند وێستگەیەک لە تۆماری کارگە، لە یەکەم مێزی کارەوە تا مێزەکانی پشکنینی ئەمڕۆ.",
                ["storyPrev"] = "پێشوو",
                ["storyNext"] = "دواتر",
                ["craftTitle"] = "یەک پسپۆڕی لە سەرەتاوە",
                ["craftSub"] = "سیستەمی سووتەمەنیی بزوێنەری دیزڵ: پەمپ، نۆزل، و هەرچی لە نێوانیاندایە.",
                ["c1Title"] = "پەمپی ڕیزی",
                ["c1Desc"] = "هەڵوەشاندنەوەی تەواو، گۆڕینی پلەنجەر و ڤاڵڤەکان، و ڕێکخستنی بڕی سووتەمەنی و کات بۆ هەر سلندەرێک.",
                ["c1Spec"] = "بارهەڵگر، تراکتۆر، مۆلیدە",
                ["c2Title"] = "پەمپی خولاوە",
                ["c2Desc"] = "چاککردنەوەی پەمپی دابەشکەری خولاوە، لەگەڵ گۆڕینی سەری دابەشکەر و ڕێکخەری خێرایی و واشەرەکان.",
                ["c2Spec"] = "VE و DPA *",
                ["c3Title"] = "نۆزل و ئینجێکتەر",
                ["c3Desc"] = "پشکنینی پەستانی کرانەوە و شێوەی پرژاندن و دزەکردن، و گۆڕینی دەرزی و سەرەکان و دووبارە ڕێکخستن.",
                ["c3Spec"] = "میکانیکی و ئەلیکترۆنی *",
                ["c4Title"] = "سیستەمی کۆمن ڕێڵ *",
                ["c4Desc"] = "پشکنین و چاککردنەوەی پەمپی پەستانی بەرز و ئینجێکتەری ئەلیکترۆنی. *",
                ["c4Spec"] = "Bosch، Denso، Delphi *",
                ["c5Title"] = "پشکنین و ڕێکخستن",
                ["c5Desc"] = "هەموو پەمپێک لەسەر مێزی پشکنین کار پێدەکرێت و بەپێی ژمارەکانی کۆمپانیای دروستکەر ڕێک دەخرێت پێش ئەوەی کارگە جێبهێڵێت.",
                ["c5Spec"] = "مێزی پشکنین بۆ هەر جۆرێک *",
                ["c6Title"] = "فرۆشتن و پارچەی یەدەگ",
                ["c6Desc"] = "پەمپ و نۆزلی نوێ و نوێکراوە، و پارچەی یەدەگ بۆ زۆربەی ئەو جۆرانەی لە عێراق بەکاردێن.",
                ["c6Spec"] = "گەرەنتی لەسەر کار *",
                ["procTitle"] = "پەمپ چۆن بە کارگەدا تێدەپەڕێت",
                ["p1Title"] = "وەرگرتن و دەستنیشانکردن",
                ["p1Desc"] = "گوێ لە کێشەکە دەگرین و پێش کردنەوەی پەمپەکە دەیپشکنین.",
                ["p2Title"] = "هەڵوەشاندنەوە و گۆڕین",
                ["p2Desc"] = "بە تەواوی هەڵدەوەشێتەوە و پارچە سواوەکان دەگۆڕدرێن.",
                ["p3Title"] = "ڕێکخستن لەسەر مێز",
                ["p3Desc"] = "بڕ و کات لەسەر مێزی پشکنین ڕێک دەخرێن.",
                ["p4Title"] = "ڕادەستکردن",
                ["p4Desc"] = "ئامادە بۆ بەستن ڕادەست دەکرێت، لەگەڵ گەرەنتی لەسەر کار. *",
                ["reachTitle"] = "لە هەموو پارێزگاکانەوە بۆ نەجەف",
                ["reachBody"] = "پەمپ و نۆزل لە کارگە و شۆفێر و جووتیارانی سەرانسەری وڵاتەوە دەگاتە لامان، و دوای پشکنین و ڕێکخستن دەگەڕێتەوە بۆیان.",
                ["reachNote"] = "* پارێزگاکانی سەر نەخشەکە تەنها بۆ ڕوونکردنەوەن تا داتای کارگە زیاد دەکرێت.",
                ["riverEuphrates"] = "فورات",
                ["riverTigris"] = "دیجلە",
                ["famTitle"] = "ڕاستەوخۆ لەگەڵ خاوەنانی کارگە قسە بکە",
                ["famBody"] = "کارگە لەلایەن حاجی سەمیر محەمەد کەرمانی و کوڕەکانیەوە بەڕێوەدەبرێت. ئەوەی وەڵامی پەیوەندییەکەت دەداتەوە، هەر ئەوە لەسەر پەمپەکەت کار دەکات.",
                ["visitTitle"] = "سەردانی کارگە بکە",
                ["visitAddrLabel"] = "ناونیشان",
                ["visitAddr"] = "نەجەفی ئەشرەف، گەڕەکی پیشەسازی، ناوەڕاستی شەقامی ئامادەیی پیشەسازی، بەرامبەر زانکۆی کوفە",
                ["visitHoursLabel"] = "کاتەکانی کار",
                ["visitHours"] = "لە شەممەوە تا پێنجشەممە، 8ی بەیانی تا 6ی ئێوارە. هەینی پشووە. *",
                ["visitDirections"] = "لە Google Maps بیکەرەوە",
                ["visitMapNote"] = "شوێنی نیشانەکە نزیکەییە *",
                ["mapPinTitle"] = "کارگەی فوراتی ناوەڕاست"
            };

            return data;
        }

        private static List<MapPin> DefaultMapPins()
        {
            double lat = Pages.WorkshopPin[0], lng = Pages.WorkshopPin[1];
            string ar = "النجف الأشرف - الحي الصناعي - منتصف شارع إعدادية الصناعة";
            string en = "Najaf Al-Ashraf - Industrial District - Middle of Industrial High School Street";
            string ku = "نەجەفی ئەشرەف - گەڕەکی پیشەسازی - ناوەڕاستی شەقامی ئامادەیی پیشەسازی";
            return new List<MapPin>
            {
                new MapPin { Id = 1, NameAr = "قسم الاستقبال والإدارة", NameEn = "Reception & Management", NameKu = "بەشی وەرگرتن و بەڕێوەبردن", AddressAr = ar, AddressEn = en, AddressKu = ku, Phone = Pages.Phones[0][1], Latitude = lat, Longitude = lng, IsApproved = true },
                new MapPin { Id = 2, NameAr = "قسم تصليح البمبات", NameEn = "Pump Repair Department", NameKu = "بەشی چاککردنەوەی پەمپ", AddressAr = ar, AddressEn = en, AddressKu = ku, Phone = Pages.Phones[1][1], Latitude = lat + 0.00012, Longitude = lng + 0.00016, IsApproved = true },
                new MapPin { Id = 3, NameAr = "قسم النوزلات والحاقنات", NameEn = "Nozzles & Injectors Department", NameKu = "بەشی نۆزل و ئینجێکتەر", AddressAr = ar, AddressEn = en, AddressKu = ku, Phone = Pages.Phones[2][1], Latitude = lat - 0.00012, Longitude = lng - 0.00016, IsApproved = true }
            };
        }

        private static List<ArchivalItem> DefaultArchivalAlbum()
        {
            return new List<ArchivalItem>
            {
                new ArchivalItem { Id = 1, Year = "1945", ImageUrl = "https://images.unsplash.com/photo-1514443031610-8c063c7a9822?w=900&h=675&fit=crop&q=70", CaptionAr = "تأسيس الورشة في النجف الأشرف، أول ورشة في العراق لتصليح مضخات حقن الوقود.", CaptionEn = "The workshop opens in Najaf, the first in Iraq to repair fuel injection pumps.", CaptionKu = "کارگە لە نەجەفی ئەشرەف دەکرێتەوە، یەکەم کارگە لە عێراق بۆ چاککردنەوەی پەمپی سووتەمەنی." },
                new ArchivalItem { Id = 2, Year = "1965", ImageUrl = "https://images.unsplash.com/photo-1579107821380-a2f5df32d67f?w=900&h=675&fit=crop&q=70", CaptionAr = "توسعة الورشة وإضافة مخرطة لتصنيع القطع محلياً. *", CaptionEn = "The workshop expands and adds a lathe to make parts in-house. *", CaptionKu = "کارگە فراوان دەکرێت و خەرادەیەک زیاد دەکرێت بۆ دروستکردنی پارچە لە ناوخۆ. *" },
                new ArchivalItem { Id = 3, Year = "1980", ImageUrl = "https://images.unsplash.com/photo-1667316337036-5b82f7d6535c?w=900&h=675&fit=crop&q=70", CaptionAr = "الانتقال إلى الموقع الحالي في الحي الصناعي. *", CaptionEn = "The move to today's premises in the Industrial District. *", CaptionKu = "گواستنەوە بۆ شوێنی ئێستا لە گەڕەکی پیشەسازی. *" },
                new ArchivalItem { Id = 4, Year = "1995", ImageUrl = "https://images.unsplash.com/photo-1759148413911-5ded7ed2ec06?w=900&h=675&fit=crop&q=70", CaptionAr = "الجيل الثاني يتسلّم الإدارة. *", CaptionEn = "The second generation takes over the management. *", CaptionKu = "نەوەی دووەم بەڕێوەبردن وەردەگرێت. *" },
                new ArchivalItem { Id = 5, Year = "2010", ImageUrl = "https://images.unsplash.com/photo-1565954786194-d22abeaac3ae?w=900&h=675&fit=crop&q=70", CaptionAr = "إدخال طاولات فحص ومعايرة حديثة للبمبات والنوزلات. *", CaptionEn = "Modern test and calibration benches are installed for pumps and nozzles. *", CaptionKu = "مێزی پشکنین و ڕێکخستنی نوێ بۆ پەمپ و نۆزل دادەنرێت. *" },
                new ArchivalItem { Id = 6, Year = "2026", ImageUrl = "https://images.unsplash.com/photo-1554231063-fef7ed86c0b7?w=900&h=675&fit=crop&q=70", CaptionAr = "الورشة اليوم بإدارة الحاج سمير محمد الكرماني وأولاده.", CaptionEn = "The workshop today, run by Hajj Samir Mohammed Al-Kermani and his sons.", CaptionKu = "کارگە ئەمڕۆ، بە بەڕێوەبەرایەتیی حاجی سەمیر محەمەد کەرمانی و کوڕەکانی." }
            };
        }

        // Stock photos the first version shipped with, and what replaces each of them.
        private static readonly Dictionary<string, string> LegacyPhotos = new Dictionary<string, string>
        {
            ["photo-1581093588401-fbb62a02f120"] = "photo-1727292486169-33eba0865c99",
            ["photo-1581092335876-1b5a6b5b0b1f"] = "photo-1759148413911-5ded7ed2ec06",
            ["photo-1485827404703-89b55fcc595e"] = "photo-1554231063-fef7ed86c0b7",
            ["photo-1504328345606-18bbc8c9d7d1"] = "photo-1579107821380-a2f5df32d67f",
            ["photo-1518770660439-4636190af475"] = "photo-1666634157070-6fd830fb5672",
            ["photo-1504639725590-34d0984388bd"] = "photo-1514443031610-8c063c7a9822"
        };

        private static string SwapLegacyPhoto(string url)
        {
            if (string.IsNullOrEmpty(url)) return url ?? "";
            foreach (var kv in LegacyPhotos)
                if (url.Contains(kv.Key)) return url.Replace(kv.Key, kv.Value);
            return url;
        }

        private static void KeepCopy(string path)
        {
            try { if (File.Exists(path)) File.Copy(path, path + ".before-redesign", true); } catch { }
        }

        // Runs once at start-up. A server that still holds the first version's saved
        // content (the CONSAN template text, the 1980 date, the old third phone number,
        // the placeholder map position) is brought up to date; anything already edited
        // for the new site is left alone.
        private static void UpgradeLegacyData()
        {
            try
            {
                bool oldContent = _content == null || _content.Translations == null
                    || !_content.Translations.ContainsKey("ar")
                    || !_content.Translations["ar"].ContainsKey("standTitle");
                if (oldContent)
                {
                    KeepCopy(ContentPath);
                    _content = DefaultContent();
                    Persist(ContentPath, _content);
                }

                bool oldArchive = _archival == null || _archival.Count == 0
                    || _archival.Any(a => a.Year == "1900" || LegacyPhotos.Keys.Any(k => (a.ImageUrl ?? "").Contains(k)));
                if (oldArchive)
                {
                    KeepCopy(ArchivalPath);
                    _archival = DefaultArchivalAlbum();
                    Persist(ArchivalPath, _archival);
                }

                bool pinsChanged = false;
                int nextPinId = _mapPins.Count > 0 ? _mapPins.Max(p => p.Id) + 1 : 1;
                for (int i = 0; i < _mapPins.Count; i++)
                {
                    var pin = _mapPins[i];
                    if (pin.Id == 0) { pin.Id = nextPinId++; pinsChanged = true; }
                    if (pin.Phone == "07801048314") { pin.Phone = Pages.Phones[2][1]; pinsChanged = true; }
                    bool placeholder = pin.Latitude > 31.995 && pin.Latitude < 32.003 && pin.Longitude > 44.325 && pin.Longitude < 44.335;
                    if (placeholder)
                    {
                        pin.Latitude = Pages.WorkshopPin[0] + (i % 3 - 1) * 0.00012;
                        pin.Longitude = Pages.WorkshopPin[1] + (i % 3 - 1) * 0.00016;
                        pinsChanged = true;
                    }
                }
                if (pinsChanged) Persist(MapPinsPath, _mapPins);

                bool cardsChanged = false;
                int nextCardId = _explorerCards.Count > 0 ? _explorerCards.Max(x => x.Id) + 1 : 1;
                foreach (var card in _explorerCards)
                {
                    if (card.Id == 0) { card.Id = nextCardId++; cardsChanged = true; }
                    string swapped = SwapLegacyPhoto(card.ImageUrl);
                    if (swapped != card.ImageUrl) { card.ImageUrl = swapped; cardsChanged = true; }
                }
                if (cardsChanged) Persist(ExplorerCardsPath, _explorerCards);

                bool archiveIds = false;
                int nextItemId = _archival.Count > 0 ? _archival.Max(x => x.Id) + 1 : 1;
                foreach (var item in _archival)
                    if (item.Id == 0) { item.Id = nextItemId++; archiveIds = true; }
                if (archiveIds) Persist(ArchivalPath, _archival);

                var oldPhrases = new Dictionary<string, string>
                {
                    ["بخبرة تمتد لأكثر من 40 عاماً"] = "من ورشة تعمل منذ عام 1945",
                    ["backed by over 40 years of experience"] = "from a workshop at work since 1945",
                    ["بە ئەزموونی زیاتر لە 40 ساڵ"] = "لە کارگەیەک کە لە 1945ـەوە کار دەکات"
                };
                bool indChanged = false;
                if (_indContent != null && _indContent.Translations != null)
                {
                    foreach (var lang in _indContent.Translations.Values)
                    {
                        foreach (var key in lang.Keys.ToList())
                        {
                            string v = lang[key] ?? "";
                            foreach (var kv in oldPhrases)
                                if (v.Contains(kv.Key)) { v = v.Replace(kv.Key, kv.Value); indChanged = true; }
                            lang[key] = v;
                        }
                    }
                }
                if (indChanged) Persist(IndustrialContentPath, _indContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Content upgrade skipped: " + ex.Message);
            }
        }
    }
}
