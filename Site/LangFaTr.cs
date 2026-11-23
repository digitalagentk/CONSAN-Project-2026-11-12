// =============================================================================
// ME Workshop — Middle Euphrates Workshop · Persian (fa) and Turkish (tr) copy
// The built-in Persian and Turkish text for every page. Like the other languages,
// all of it can be edited from the admin dashboard once the site is running.
// A server that already holds saved content gets these added on start-up
// (EnsureFaTr): only empty Persian/Turkish fields are filled, nothing is replaced.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsanWebApp
{
    public static partial class Store
    {
        // ---------------------------------------------------------------
        // Home page, header, footer, contact sheet, gallery
        // ---------------------------------------------------------------
        internal static Dictionary<string, string> SiteFa() => new Dictionary<string, string>
        {
            ["cookieText"] = "ما از کوکی‌ها استفاده می‌کنیم تا بدانیم چه کسانی از سایت بازدید می‌کنند و خدماتمان را بهتر کنیم.",
            ["cookieAccept"] = "موافقم",
            ["cookieDecline"] = "نمی‌پذیرم",
            ["waBadgeText"] = "در واتس‌اپ پیام دهید",
            ["waBadgeLabel"] = "ارسال پیام به کارگاه در واتس‌اپ",
            ["navGallery"] = "گالری",
            ["galleryTitle"] = "گالری کارگاه",
            ["gallerySub"] = "عکس‌ها و ویدیوهایی از داخل کارگاه: پمپ‌ها و سوزن‌ها روی میز تست و کار روزانه.",
            ["galleryHomeTitle"] = "درون کارگاه",
            ["galleryViewAll"] = "دیدن همه گالری",
            ["galleryAll"] = "همه",
            ["galleryPhotos"] = "عکس‌ها",
            ["galleryVideos"] = "ویدیوها",
            ["galleryEmpty"] = "هنوز عکس یا ویدیویی وجود ندارد.",
            ["galleryClose"] = "بستن",
            ["galleryPrev"] = "قبلی",
            ["galleryNext"] = "بعدی",
            ["navHome"] = "خانه",
            ["navAbout"] = "تاریخچه ما",
            ["navServices"] = "چه چیزی تعمیر می‌کنیم",
            ["navIndustrial"] = "خدمات صنعتی",
            ["navExplorer"] = "کاوشگر",
            ["navGuide"] = "راهنما",
            ["navCta"] = "درخواست خدمت",
            ["navLogin"] = "ورود کارکنان",
            ["brandFull"] = "کارگاه فرات میانه",
            ["footerText"] = "کارگاه فرات میانه پمپ‌ها و سوزن‌های انژکتور دیزل را تعمیر می‌کند و می‌فروشد. نخستین کارگاه تعمیر پمپ انژکتور سوخت در عراق، از سال ۱۹۴۵ در نجف.",
            ["footerLinksTitle"] = "سایت",
            ["footerCallTitle"] = "با ما تماس بگیرید",
            ["footerCopy"] = "کارگاه فرات میانه. همه حقوق محفوظ است.",
            ["footerMock"] = "* اطلاعات نمونه است و با اطلاعات واقعی کارگاه جایگزین می‌شود.",
            ["contactBadgeText"] = "تماس با کارگاه",
            ["contactTitle"] = "شماره‌های کارگاه",
            ["contactLabel1"] = "ابومصطفی",
            ["contactLabel2"] = "ابورضا",
            ["contactLabel3"] = "ابوعلی",
            ["callLabel"] = "تماس",
            ["waLabel"] = "واتس‌اپ",
            ["heroWhere"] = "نجف، منطقه صنعتی",
            ["heroTitle"] = "کارگاه فرات میانه",
            ["heroLead"] = "نخستین کارگاه تعمیر پمپ انژکتور سوخت در عراق. از سال ۱۹۴۵ پمپ‌ها و سوزن‌های دیزل را برای صاحبان موتور در سراسر عراق تعمیر، تنظیم و فروخته‌ایم.",
            ["heroCtaCall"] = "تماس با کارگاه",
            ["heroCtaService"] = "درخواست خدمت",
            ["plateTypeLabel"] = "تخصص",
            ["plateType"] = "پمپ و سوزن دیزل",
            ["plateEstLabel"] = "تأسیس",
            ["plateCityLabel"] = "مکان",
            ["plateCity"] = "نجف، عراق",
            ["plateMgmtLabel"] = "مدیریت",
            ["plateMgmt"] = "حاج سمیر محمد کرمانی و پسرانش",
            ["scaleYears"] = "{n} سال کار پیوسته",
            ["standTitle"] = "نامی که صاحبان موتور در سراسر عراق می‌شناسند",
            ["standBody"] = "در سال ۱۹۴۵ هیچ کارگاهی در عراق پمپ انژکتور سوخت تعمیر نمی‌کرد. این کارگاه در نجف باز شد و نخستین شد. از آن زمان رانندگان کامیون، کشاورزان و صاحبان ژنراتور، از بصره تا موصل و نسل به نسل، پمپ‌هایشان را به اینجا آورده‌اند.",
            ["fact1Label"] = "تأسیس در نجف",
            ["fact2Label"] = "سال کار پیوسته",
            ["fact3Num"] = "3*",
            ["fact3Label"] = "نسل از یک خانواده",
            ["fact4Num"] = "18*",
            ["fact4Label"] = "استانی که مشتریان ما از آن می‌آیند",
            ["storyTitle"] = "از ۱۹۴۵ تا امروز",
            ["storySub"] = "لحظه‌هایی از کارنامه کارگاه، از نخستین میز کار تا میزهای تست امروز.",
            ["storyPrev"] = "قبلی",
            ["storyNext"] = "بعدی",
            ["craftTitle"] = "یک تخصص از همان آغاز",
            ["craftSub"] = "سیستم انژکتور سوخت موتور دیزل: پمپ، سوزن و هر آنچه میان آن‌هاست.",
            ["c1Title"] = "پمپ‌های انژکتور خطی",
            ["c1Desc"] = "باز کردن کامل، پیستون و سوپاپ تحویل نو، سپس تنظیم مقدار سوخت و تایمینگ سیلندر به سیلندر.",
            ["c1Spec"] = "کامیون، تراکتور، ژنراتور",
            ["c2Title"] = "پمپ‌های دوار",
            ["c2Desc"] = "پمپ‌های نوع توزیعی اورهال می‌شوند و در صورت نیاز سرپمپ، گاورنر و کاسه‌نمدها تعویض می‌شوند.",
            ["c2Spec"] = "VE و DPA *",
            ["c3Title"] = "سوزن‌ها و انژکتورها",
            ["c3Desc"] = "فشار بازشدن، الگوی پاشش و نشت برگشتی آزمایش می‌شود؛ سوزن‌ها و نازل‌ها تعویض و دوباره تنظیم می‌شوند.",
            ["c3Spec"] = "مکانیکی و الکترونیکی *",
            ["c4Title"] = "سیستم‌های کامن ریل *",
            ["c4Desc"] = "پمپ‌های فشار بالا و انژکتورهای الکترونیکی آزمایش و تعمیر می‌شوند. *",
            ["c4Spec"] = "Bosch، Denso، Delphi *",
            ["c5Title"] = "تست و کالیبراسیون",
            ["c5Desc"] = "هر پمپ پیش از تحویل روی میز تست کار می‌کند و طبق ارقام سازنده کالیبره می‌شود.",
            ["c5Spec"] = "یک میز تست برای هر نوع پمپ *",
            ["c6Title"] = "فروش و قطعات یدکی",
            ["c6Desc"] = "پمپ و سوزن نو و بازسازی‌شده، و قطعات بیشتر انواع رایج در عراق.",
            ["c6Spec"] = "کار با ضمانت *",
            ["procTitle"] = "یک پمپ در کارگاه چه مسیری را طی می‌کند",
            ["p1Title"] = "پذیرش و عیب‌یابی",
            ["p1Desc"] = "ایراد را از شما می‌شنویم و پیش از باز کردن، پمپ را آزمایش می‌کنیم.",
            ["p2Title"] = "باز کردن و تعویض",
            ["p2Desc"] = "کاملاً باز می‌شود و قطعات فرسوده تعویض می‌شوند.",
            ["p3Title"] = "کالیبراسیون روی میز تست",
            ["p3Desc"] = "مقدار سوخت و تایمینگ روی میز تست تنظیم می‌شود.",
            ["p4Title"] = "تحویل",
            ["p4Desc"] = "آماده نصب، همراه با ضمانت کار تحویل داده می‌شود. *",
            ["reachTitle"] = "از همه استان‌ها به نجف",
            ["reachBody"] = "پمپ‌ها و سوزن‌ها از تعمیرگاه‌ها، رانندگان و کشاورزان سراسر کشور به دست ما می‌رسند و آزمایش‌شده و کالیبره به آن‌ها برمی‌گردند.",
            ["reachNote"] = "* استان‌های نشان داده‌شده تا افزوده شدن آمار خود کارگاه، نمونه هستند.",
            ["riverEuphrates"] = "فرات",
            ["riverTigris"] = "دجله",
            ["famTitle"] = "مستقیم با صاحبان کارگاه صحبت کنید",
            ["famBody"] = "کارگاه را حاج سمیر محمد کرمانی و پسرانش اداره می‌کنند. کسی که به تماس شما پاسخ می‌دهد، همان کسی است که روی پمپ شما کار می‌کند.",
            ["visitTitle"] = "از کارگاه دیدن کنید",
            ["visitAddrLabel"] = "نشانی",
            ["visitAddr"] = "نجف، منطقه صنعتی، میانه خیابان هنرستان صنعتی، روبه‌روی دانشگاه کوفه",
            ["visitHoursLabel"] = "ساعات کاری",
            ["visitHours"] = "شنبه تا پنج‌شنبه، ۸ صبح تا ۶ عصر. جمعه تعطیل. *",
            ["visitDirections"] = "باز کردن در Google Maps",
            ["visitMapNote"] = "مکان نشانگر تقریبی است *",
            ["mapPinTitle"] = "کارگاه فرات میانه"
        };

        internal static Dictionary<string, string> SiteTr() => new Dictionary<string, string>
        {
            ["cookieText"] = "Sitemizi kimlerin ziyaret ettiğini anlamak ve hizmetimizi geliştirmek için çerez kullanıyoruz.",
            ["cookieAccept"] = "Kabul et",
            ["cookieDecline"] = "Reddet",
            ["waBadgeText"] = "WhatsApp'tan yazın",
            ["waBadgeLabel"] = "Atölyeye WhatsApp'tan mesaj gönderin",
            ["navGallery"] = "Galeri",
            ["galleryTitle"] = "Atölye galerisi",
            ["gallerySub"] = "Atölyenin içinden fotoğraf ve videolar: test tezgâhındaki pompalar ve enjektörler, günlük çalışma.",
            ["galleryHomeTitle"] = "Atölyenin içinden",
            ["galleryViewAll"] = "Galerinin tamamını gör",
            ["galleryAll"] = "Tümü",
            ["galleryPhotos"] = "Fotoğraflar",
            ["galleryVideos"] = "Videolar",
            ["galleryEmpty"] = "Henüz fotoğraf veya video yok.",
            ["galleryClose"] = "Kapat",
            ["galleryPrev"] = "Önceki",
            ["galleryNext"] = "Sonraki",
            ["navHome"] = "Ana sayfa",
            ["navAbout"] = "Tarihçemiz",
            ["navServices"] = "Neleri onarıyoruz",
            ["navIndustrial"] = "Endüstriyel hizmetler",
            ["navExplorer"] = "Keşfet",
            ["navGuide"] = "Rehber",
            ["navCta"] = "Servis talebi",
            ["navLogin"] = "Personel girişi",
            ["brandFull"] = "Orta Fırat Atölyesi",
            ["footerText"] = "Orta Fırat Atölyesi dizel enjeksiyon pompaları ve enjektörleri onarır ve satar. Irak'ta yakıt enjeksiyon pompası onaran ilk atölye, 1945'ten beri Necef'te.",
            ["footerLinksTitle"] = "Site",
            ["footerCallTitle"] = "Bizi arayın",
            ["footerCopy"] = "Orta Fırat Atölyesi. Tüm hakları saklıdır.",
            ["footerMock"] = "* Örnek bilgilerdir, atölyenin gerçek bilgileriyle değiştirilecektir.",
            ["contactBadgeText"] = "Atölyeyi arayın",
            ["contactTitle"] = "Atölye numaraları",
            ["contactLabel1"] = "Ebu Mustafa",
            ["contactLabel2"] = "Ebu Rıza",
            ["contactLabel3"] = "Ebu Ali",
            ["callLabel"] = "Ara",
            ["waLabel"] = "WhatsApp",
            ["heroWhere"] = "Necef, Sanayi Bölgesi",
            ["heroTitle"] = "Orta Fırat Atölyesi",
            ["heroLead"] = "Irak'ta yakıt enjeksiyon pompası onaran ilk atölye. 1945'ten beri Irak'ın dört bir yanındaki motor sahipleri için dizel pompaları ve enjektörleri onarıyor, ayarlıyor ve satıyoruz.",
            ["heroCtaCall"] = "Atölyeyi arayın",
            ["heroCtaService"] = "Servis talebi",
            ["plateTypeLabel"] = "Uzmanlık",
            ["plateType"] = "Dizel pompa ve enjektörler",
            ["plateEstLabel"] = "Kuruluş",
            ["plateCityLabel"] = "Konum",
            ["plateCity"] = "Necef, Irak",
            ["plateMgmtLabel"] = "Yönetim",
            ["plateMgmt"] = "Hacı Samir Muhammed el-Kirmani ve oğulları",
            ["scaleYears"] = "{n} yıldır kesintisiz çalışma",
            ["standTitle"] = "Irak'ın her yerinde motor sahiplerinin bildiği bir isim",
            ["standBody"] = "1945'te Irak'ta yakıt enjeksiyon pompası onaran tek bir atölye yoktu. Bu atölye Necef'te açıldı ve ilk oldu. O günden beri kamyon şoförleri, çiftçiler ve jeneratör sahipleri, Basra'dan Musul'a, kuşaktan kuşağa pompalarını buraya getiriyor.",
            ["fact1Label"] = "Necef'te kuruldu",
            ["fact2Label"] = "yıldır kesintisiz çalışma",
            ["fact3Num"] = "3*",
            ["fact3Label"] = "aynı aileden kuşak",
            ["fact4Num"] = "18*",
            ["fact4Label"] = "müşterilerimizin geldiği il",
            ["storyTitle"] = "1945'ten bugüne",
            ["storySub"] = "Atölyenin kayıtlarından anlar: ilk tezgâhtan bugünün test tezgâhlarına.",
            ["storyPrev"] = "Önceki",
            ["storyNext"] = "Sonraki",
            ["craftTitle"] = "Başından beri tek bir uzmanlık",
            ["craftSub"] = "Dizel motorun yakıt enjeksiyon sistemi: pompa, enjektör ve ikisi arasındaki her şey.",
            ["c1Title"] = "Sıra tipi enjeksiyon pompaları",
            ["c1Desc"] = "Tamamen sökülür, yeni pistonlar ve basma valfleri takılır, ardından yakıt miktarı ve avans silindir silindir ayarlanır.",
            ["c1Spec"] = "Kamyon, traktör, jeneratör",
            ["c2Title"] = "Döner tip pompalar",
            ["c2Desc"] = "Distribütör tipi pompalar revizyondan geçirilir; gerekirse kafa, regülatör ve keçeler değiştirilir.",
            ["c2Spec"] = "VE ve DPA *",
            ["c3Title"] = "Enjektörler ve memeler",
            ["c3Desc"] = "Açılma basıncı, püskürtme şekli ve geri kaçak test edilir; iğneler ve memeler değiştirilip yeniden ayarlanır.",
            ["c3Spec"] = "Mekanik ve elektronik *",
            ["c4Title"] = "Common rail sistemleri *",
            ["c4Desc"] = "Yüksek basınç pompaları ve elektronik enjektörler test edilir ve onarılır. *",
            ["c4Spec"] = "Bosch, Denso, Delphi *",
            ["c5Title"] = "Test ve kalibrasyon",
            ["c5Desc"] = "Her pompa teslimden önce test tezgâhında çalıştırılır ve üretici değerlerine göre kalibre edilir.",
            ["c5Spec"] = "Her pompa tipi için bir tezgâh *",
            ["c6Title"] = "Satış ve yedek parça",
            ["c6Desc"] = "Yeni ve yenilenmiş pompalar ve enjektörler, Irak'ta kullanılan çoğu tip için parçalar.",
            ["c6Spec"] = "Garantili işçilik *",
            ["procTitle"] = "Bir pompa atölyede nasıl ilerler",
            ["p1Title"] = "Kabul ve arıza tespiti",
            ["p1Desc"] = "Arızayı sizden dinler, pompayı açmadan önce test ederiz.",
            ["p2Title"] = "Sökme ve değiştirme",
            ["p2Desc"] = "Tamamen sökülür; aşınmış parçalar değiştirilir.",
            ["p3Title"] = "Tezgâhta kalibrasyon",
            ["p3Desc"] = "Miktar ve avans test tezgâhında ayarlanır.",
            ["p4Title"] = "Teslim",
            ["p4Desc"] = "Takılmaya hazır, işçilik garantisiyle teslim edilir. *",
            ["reachTitle"] = "Her ilden Necef'e",
            ["reachBody"] = "Pompalar ve enjektörler ülkenin dört bir yanındaki servislerden, şoförlerden ve çiftçilerden bize ulaşır; test edilmiş ve kalibre edilmiş olarak geri döner.",
            ["reachNote"] = "* Gösterilen iller, atölyenin kendi rakamları eklenene kadar örnektir.",
            ["riverEuphrates"] = "Fırat",
            ["riverTigris"] = "Dicle",
            ["famTitle"] = "Doğrudan sahipleriyle konuşun",
            ["famBody"] = "Atölyeyi Hacı Samir Muhammed el-Kirmani ve oğulları yönetiyor. Telefonunuzu açan kişi, pompanız üzerinde çalışacak olan kişidir.",
            ["visitTitle"] = "Atölyeyi ziyaret edin",
            ["visitAddrLabel"] = "Adres",
            ["visitAddr"] = "Necef, Sanayi Bölgesi, Endüstri Meslek Lisesi Caddesi'nin ortası, Kufe Üniversitesi'nin karşısı",
            ["visitHoursLabel"] = "Çalışma saatleri",
            ["visitHours"] = "Cumartesiden perşembeye, 08:00–18:00. Cuma kapalı. *",
            ["visitDirections"] = "Google Haritalar'da aç",
            ["visitMapNote"] = "İşaretin konumu yaklaşıktır *",
            ["mapPinTitle"] = "Orta Fırat Atölyesi"
        };

        // ---------------------------------------------------------------
        // Industrial services page
        // ---------------------------------------------------------------
        internal static Dictionary<string, string> IndustrialFa() => new Dictionary<string, string>
        {
            ["navHome"] = "خانه",
            ["navAbout"] = "درباره ما",
            ["navServices"] = "خدمات ما",
            ["navIndustrial"] = "خدمات صنعتی",
            ["navExplorer"] = "کاوشگر",
            ["navGuide"] = "راهنمای جامع",
            ["indVisitTag"] = "به ما سر بزنید",
            ["indVisitTitle"] = """مکان ما در <span class="highlight">منطقه صنعتی، نجف اشرف</span>""",
            ["indSearchPlaceholder"] = "جستجو بر اساس نام، نشانی یا شماره تلفن...",
            ["indColName"] = "بخش / نام",
            ["indColAddress"] = "نشانی",
            ["indColPhone"] = "شماره تلفن",
            ["indHoursNote"] = "ساعات کاری: هر روز از شنبه تا پنج‌شنبه، ۸ صبح تا ۶ عصر",
            ["indHighlightsTag"] = "خدمات ما",
            ["indHighlightsTitle"] = """آنچه در <span class="highlight">کارگاه فرات میانه</span> ارائه می‌دهیم""",
            ["indHighlightsSub"] = "راهکارهای صنعتی جامع از کارگاهی که از سال ۱۹۴۵ فعال است",
            ["indHl1Title"] = "پاسخ سریع",
            ["indHl1Desc"] = "تیمی آماده برای دریافت و رسیدگی به درخواست‌های شما در کوتاه‌ترین زمان",
            ["indHl2Title"] = "تجهیزات پیشرفته",
            ["indHl2Desc"] = "از جدیدترین دستگاه‌های تراش و ابزارهای عیب‌یابی برای دقت در هر کار استفاده می‌کنیم",
            ["indHl3Title"] = "کیفیت تضمینی",
            ["indHl3Desc"] = "همه کارهای صنعتی ما با بالاترین استانداردهای جهانی کیفیت تضمین می‌شوند",
            ["indFeaturesTag"] = "خدمات صنعتی ما",
            ["indFeaturesTitle"] = """زمینه‌های <span class="highlight">تخصص</span>""",
            ["indFeaturesSub"] = "خدمات صنعتی دقیق را با جدیدترین تجهیزات و دانش فنی ارائه می‌دهیم",
            ["indF1Title"] = "تراشکاری سنگین",
            ["indF1Desc"] = "ماشین‌کاری و تراشکاری دقیق قطعات صنعتی سنگین با جدیدترین دستگاه‌های تراش و تجهیزات تخصصی",
            ["indF2Title"] = "ساخت قطعات فلزی",
            ["indF2Desc"] = "طراحی و ساخت سفارشی قطعات و سازه‌های فلزی برای نیاز کارخانه‌ها و واحدهای صنعتی",
            ["indF3Title"] = "تعمیر ماشین‌آلات صنعتی",
            ["indF3Desc"] = "بازرسی، تعمیر و نگهداری ماشین‌آلات سنگین صنعتی برای تداوم تولید",
            ["backHomeLink"] = "← بازگشت به خانه",
            ["indNoResults"] = "نتیجه‌ای پیدا نشد",
            ["addLocationBtn"] = "افزودن مکان جدید روی نقشه",
            ["modalTitle"] = "افزودن مکان جدید",
            ["modalName"] = "نام بخش / کارگاه",
            ["modalAddress"] = "نشانی",
            ["modalPhone"] = "شماره تلفن",
            ["modalConfirm"] = "تأیید و افزودن مکان",
            ["modalCancel"] = "انصراف",
            ["submitSuccess"] = "مکان شما با موفقیت ارسال شد و پس از تأیید مدیریت در جدول و نقشه نمایش داده می‌شود."
        };

        internal static Dictionary<string, string> IndustrialTr() => new Dictionary<string, string>
        {
            ["navHome"] = "Ana sayfa",
            ["navAbout"] = "Hakkımızda",
            ["navServices"] = "Hizmetlerimiz",
            ["navIndustrial"] = "Endüstriyel hizmetler",
            ["navExplorer"] = "Keşfet",
            ["navGuide"] = "Kapsamlı rehber",
            ["indVisitTag"] = "Bizi ziyaret edin",
            ["indVisitTitle"] = """Konumumuz: <span class="highlight">Sanayi Bölgesi, Necef</span>""",
            ["indSearchPlaceholder"] = "Ad, adres veya telefon numarasına göre arayın...",
            ["indColName"] = "Bölüm / Ad",
            ["indColAddress"] = "Adres",
            ["indColPhone"] = "Telefon numarası",
            ["indHoursNote"] = "Çalışma saatleri: Cumartesiden perşembeye her gün, 08:00–18:00",
            ["indHighlightsTag"] = "Hizmetlerimiz",
            ["indHighlightsTitle"] = """<span class="highlight">Orta Fırat Atölyesi</span>'nde sunduklarımız""",
            ["indHighlightsSub"] = "1945'ten beri çalışan bir atölyeden kapsamlı endüstriyel çözümler",
            ["indHl1Title"] = "Hızlı yanıt",
            ["indHl1Desc"] = "Taleplerinizi en kısa sürede alıp işleme koyan hazır bir ekip",
            ["indHl2Title"] = "Gelişmiş ekipman",
            ["indHl2Desc"] = "Her işte hassasiyet için en yeni torna tezgâhlarını ve arıza tespit araçlarını kullanıyoruz",
            ["indHl3Title"] = "Garantili kalite",
            ["indHl3Desc"] = "Tüm endüstriyel işlerimiz en yüksek uluslararası kalite standartlarıyla garanti edilir",
            ["indFeaturesTag"] = "Endüstriyel hizmetlerimiz",
            ["indFeaturesTitle"] = """<span class="highlight">Uzmanlık</span> alanları""",
            ["indFeaturesSub"] = "En yeni ekipman ve teknik uzmanlıkla hassas endüstriyel hizmetler sunuyoruz",
            ["indF1Title"] = "Ağır torna işleri",
            ["indF1Desc"] = "En yeni torna tezgâhları ve özel ekipmanla ağır endüstriyel parçaların hassas işlenmesi ve tornalanması",
            ["indF2Title"] = "Metal imalatı",
            ["indF2Desc"] = "Fabrika ve tesislerin ihtiyaçlarına göre metal parça ve yapıların özel tasarımı ve imalatı",
            ["indF3Title"] = "Endüstriyel makine onarımı",
            ["indF3Desc"] = "Üretimin kesintisiz sürmesi için ağır endüstriyel makinelerin kontrolü, onarımı ve bakımı",
            ["backHomeLink"] = "← Ana sayfaya dön",
            ["indNoResults"] = "Eşleşen sonuç yok",
            ["addLocationBtn"] = "Haritaya yeni konum ekle",
            ["modalTitle"] = "Yeni konum ekle",
            ["modalName"] = "Bölüm / atölye adı",
            ["modalAddress"] = "Adres",
            ["modalPhone"] = "Telefon numarası",
            ["modalConfirm"] = "Onayla ve konumu ekle",
            ["modalCancel"] = "İptal",
            ["submitSuccess"] = "Konumunuz başarıyla gönderildi; yönetim onayladıktan sonra tabloda ve haritada görünecek."
        };

        // ---------------------------------------------------------------
        // Guide page
        // ---------------------------------------------------------------
        internal static Dictionary<string, string> GuideFa() => new Dictionary<string, string>
        {
            ["navHome"] = "خانه",
            ["navAbout"] = "درباره ما",
            ["navServices"] = "خدمات ما",
            ["navIndustrial"] = "خدمات صنعتی",
            ["navExplorer"] = "کاوشگر",
            ["navGuide"] = "راهنمای جامع",
            ["guideTag"] = "راهنمای جامع",
            ["guideTitle"] = """راهنمای <span class="highlight">جامع</span>""",
            ["guideSub"] = "راهنمایی مفصل درباره بخش‌های کارگاه، مشخصات تجهیزات، استانداردهای ایمنی و روند کار",
            ["guideDeptTitle"] = "بخش‌های کارگاه",
            ["guideSpecsTitle"] = "مشخصات ماشین‌آلات و تجهیزات",
            ["guideSafetyTitle"] = "استانداردهای ایمنی شغلی",
            ["guideFlowTitle"] = "روند کار و خدمات",
            ["guideSpecsColMachine"] = "دستگاه",
            ["guideSpecsColSpec"] = "مشخصات",
            ["guideSpecsColUse"] = "کاربرد"
        };

        internal static Dictionary<string, string> GuideTr() => new Dictionary<string, string>
        {
            ["navHome"] = "Ana sayfa",
            ["navAbout"] = "Hakkımızda",
            ["navServices"] = "Hizmetlerimiz",
            ["navIndustrial"] = "Endüstriyel hizmetler",
            ["navExplorer"] = "Keşfet",
            ["navGuide"] = "Kapsamlı rehber",
            ["guideTag"] = "Kapsamlı rehber",
            ["guideTitle"] = """<span class="highlight">Kapsamlı</span> rehber""",
            ["guideSub"] = "Atölye bölümleri, ekipman özellikleri, güvenlik standartları ve iş akışı hakkında ayrıntılı bir rehber",
            ["guideDeptTitle"] = "Atölye bölümleri",
            ["guideSpecsTitle"] = "Makine ve ekipman özellikleri",
            ["guideSafetyTitle"] = "İş güvenliği standartları",
            ["guideFlowTitle"] = "Hizmet iş akışı",
            ["guideSpecsColMachine"] = "Makine",
            ["guideSpecsColSpec"] = "Özellikler",
            ["guideSpecsColUse"] = "Kullanım"
        };

        // ---------------------------------------------------------------
        // Item lists, keyed by the built-in English text so saved copies can be matched
        // value: { Persian, Turkish }
        // ---------------------------------------------------------------
        private static readonly Dictionary<string, string[]> ItemFaTr = new Dictionary<string, string[]>
        {
            // explorer cards: titles
            ["Advanced CNC Machines *"] = new[] { "دستگاه‌های CNC پیشرفته *", "Gelişmiş CNC tezgâhları *" },
            ["Electronic Diagnostic Equipment *"] = new[] { "تجهیزات عیب‌یابی الکترونیکی *", "Elektronik arıza tespit ekipmanı *" },
            ["Fuel Injection Systems *"] = new[] { "سیستم‌های انژکتور سوخت *", "Yakıt enjeksiyon sistemleri *" },
            ["Heavy Lathes *"] = new[] { "دستگاه‌های تراش سنگین *", "Ağır torna tezgâhları *" },
            ["Quality Control Lab *"] = new[] { "آزمایشگاه کنترل کیفیت *", "Kalite kontrol laboratuvarı *" },
            ["Modern Workshop *"] = new[] { "کارگاه مدرن *", "Modern atölye *" },
            // explorer cards: descriptions
            ["High-precision digital turning and milling systems for manufacturing complex and custom parts to the highest global quality standards."] = new[] {
                "سیستم‌های تراش و فرز دیجیتال با دقت بالا برای ساخت قطعات پیچیده و سفارشی مطابق بالاترین استانداردهای جهانی کیفیت.",
                "Karmaşık ve özel parçaları en yüksek uluslararası kalite standartlarında üretmek için yüksek hassasiyetli dijital torna ve freze sistemleri." },
            ["Advanced diagnostic equipment for diesel and gasoline injection systems, with precise data analysis software for peak performance."] = new[] {
                "تجهیزات عیب‌یابی پیشرفته برای سیستم‌های انژکتور دیزل و بنزین، همراه با نرم‌افزار دقیق تحلیل داده برای بهترین عملکرد.",
                "Dizel ve benzinli enjeksiyon sistemleri için gelişmiş arıza tespit ekipmanı; en iyi performans için hassas veri analiz yazılımıyla." },
            ["Specialized expertise in the maintenance and repair of all types of electronic and mechanical injection systems for the latest engines."] = new[] {
                "تخصص در نگهداری و تعمیر انواع سیستم‌های انژکتور الکترونیکی و مکانیکی برای جدیدترین موتورها.",
                "En yeni motorlar için her tür elektronik ve mekanik enjeksiyon sisteminin bakım ve onarımında uzmanlık." },
            ["Heavy industrial lathes capable of machining large parts with high precision, ideal for manufacturing industrial spare parts."] = new[] {
                "دستگاه‌های تراش صنعتی سنگین که قطعات بزرگ را با دقت بالا ماشین‌کاری می‌کنند؛ مناسب برای ساخت قطعات یدکی صنعتی.",
                "Büyük parçaları yüksek hassasiyetle işleyebilen ağır endüstriyel torna tezgâhları; endüstriyel yedek parça üretimi için ideal." },
            ["Precision measuring instruments and rigorous testing to ensure every part meets the required technical specifications."] = new[] {
                "ابزارهای اندازه‌گیری دقیق و آزمون‌های سخت‌گیرانه تا هر قطعه با مشخصات فنی لازم مطابقت داشته باشد.",
                "Her parçanın gerekli teknik özellikleri karşıladığından emin olmak için hassas ölçüm aletleri ve sıkı testler." },
            ["A workspace equipped with the latest tools and equipment, designed to provide a safe and efficient work environment for rapid completion."] = new[] {
                "فضای کاری مجهز به جدیدترین ابزار و تجهیزات، طراحی‌شده برای محیطی ایمن و کارآمد و انجام سریع کار.",
                "En yeni alet ve ekipmanla donatılmış, işlerin hızla tamamlanması için güvenli ve verimli bir çalışma ortamı sunacak şekilde tasarlanmış bir alan." },

            // guide topics
            ["Workshop Departments"] = new[] { "بخش‌های کارگاه", "Atölye bölümleri" },
            ["Occupational Safety Standards"] = new[] { "استانداردهای ایمنی شغلی", "İş güvenliği standartları" },
            ["Service Workflow"] = new[] { "روند کار و خدمات", "Hizmet iş akışı" },
            ["The workshop includes specialized departments: Pump Repair, Nozzles & Injectors, Electronic Diagnostics, and Industrial Lathe."] = new[] {
                "کارگاه بخش‌های تخصصی دارد: تعمیر پمپ، سوزن و انژکتور، عیب‌یابی الکترونیکی و تراش صنعتی.",
                "Atölyede uzman bölümler bulunur: Pompa Onarımı, Enjektör ve Memeler, Elektronik Arıza Tespiti ve Endüstriyel Torna." },
            ["We adhere to the highest safety standards: personal protective equipment, fire extinguishers, warning signs, and emergency plans."] = new[] {
                "به بالاترین استانداردهای ایمنی پایبندیم: تجهیزات حفاظت فردی، کپسول آتش‌نشانی، تابلوهای هشدار و برنامه‌های اضطراری.",
                "En yüksek güvenlik standartlarına uyuyoruz: kişisel koruyucu donanım, yangın söndürücüler, uyarı levhaları ve acil durum planları." },
            ["We follow precise steps: request reception, inspection and diagnosis, cost estimation, work execution, quality testing, then delivery and follow-up."] = new[] {
                "گام‌های دقیقی را دنبال می‌کنیم: دریافت درخواست، بازرسی و عیب‌یابی، برآورد هزینه، انجام کار، آزمون کیفیت، سپس تحویل و پیگیری.",
                "Belirli adımları izliyoruz: talebin alınması, kontrol ve arıza tespiti, maliyet tahmini, işin yapılması, kalite testi, ardından teslim ve takip." },

            // guide equipment
            ["Nozzle Test Machine *"] = new[] { "دستگاه تست سوزن *", "Enjektör test makinesi *" },
            ["Pressure up to 3000 bar, digital control"] = new[] { "فشار تا ۳۰۰۰ بار، کنترل دیجیتال", "3000 bara kadar basınç, dijital kontrol" },
            ["Testing and calibrating diesel injectors"] = new[] { "تست و کالیبره کردن انژکتورهای دیزل", "Dizel enjektörlerin test ve kalibrasyonu" },
            ["CNC Lathe *"] = new[] { "تراش CNC *", "CNC torna *" },
            ["Diameter 500 mm, length 1500 mm, precision 0.01 mm"] = new[] { "قطر ۵۰۰ میلی‌متر، طول ۱۵۰۰ میلی‌متر، دقت ۰٫۰۱ میلی‌متر", "Çap 500 mm, uzunluk 1500 mm, hassasiyet 0,01 mm" },
            ["Manufacturing precision spare parts"] = new[] { "ساخت قطعات یدکی دقیق", "Hassas yedek parça üretimi" },
            ["Electronic Diagnostic Device *"] = new[] { "دستگاه عیب‌یاب الکترونیکی *", "Elektronik arıza tespit cihazı *" },
            ["Multi-channel, OBD-II reading"] = new[] { "چندکاناله، خواندن OBD-II", "Çok kanallı, OBD-II okuma" },
            ["Diagnosing injection system faults"] = new[] { "عیب‌یابی ایرادهای سیستم انژکتور", "Enjeksiyon sistemi arızalarının tespiti" },
            ["Heavy Lathe *"] = new[] { "تراش سنگین *", "Ağır torna *" },
            ["Diameter 1000 mm, length 3000 mm"] = new[] { "قطر ۱۰۰۰ میلی‌متر، طول ۳۰۰۰ میلی‌متر", "Çap 1000 mm, uzunluk 3000 mm" },
            ["Machining large industrial parts"] = new[] { "ماشین‌کاری قطعات بزرگ صنعتی", "Büyük endüstriyel parçaların işlenmesi" },

            // map pins
            ["Reception & Management"] = new[] { "بخش پذیرش و مدیریت", "Kabul ve Yönetim" },
            ["Pump Repair Department"] = new[] { "بخش تعمیر پمپ", "Pompa Onarım Bölümü" },
            ["Nozzles & Injectors Department"] = new[] { "بخش سوزن و انژکتور", "Enjektör ve Meme Bölümü" },
            ["ME Workshop — Middle Euphrates Workshop"] = new[] { "کارگاه فرات میانه (ME Workshop)", "Orta Fırat Atölyesi (ME Workshop)" },
            ["Najaf Al-Ashraf - Industrial District - Middle of Industrial High School Street"] = new[] {
                "نجف اشرف - منطقه صنعتی - میانه خیابان هنرستان صنعتی",
                "Necef - Sanayi Bölgesi - Endüstri Meslek Lisesi Caddesi'nin ortası" },
            ["Najaf, Industrial District, middle of Industrial High School Street, opposite the University of Kufa"] = new[] {
                "نجف، منطقه صنعتی، میانه خیابان هنرستان صنعتی، روبه‌روی دانشگاه کوفه",
                "Necef, Sanayi Bölgesi, Endüstri Meslek Lisesi Caddesi'nin ortası, Kufe Üniversitesi'nin karşısı" },

            // history timeline
            ["The workshop opens in Najaf, the first in Iraq to repair fuel injection pumps."] = new[] {
                "کارگاه در نجف گشایش می‌یابد؛ نخستین کارگاه تعمیر پمپ انژکتور سوخت در عراق.",
                "Atölye Necef'te açılır; Irak'ta yakıt enjeksiyon pompası onaran ilk atölye." },
            ["The workshop expands and adds a lathe to make parts in-house. *"] = new[] {
                "کارگاه گسترش می‌یابد و یک دستگاه تراش برای ساخت قطعات در خود کارگاه می‌افزاید. *",
                "Atölye büyür ve parçaları kendi bünyesinde üretmek için bir torna tezgâhı ekler. *" },
            ["The move to today's premises in the Industrial District. *"] = new[] {
                "انتقال به محل کنونی در منطقه صنعتی. *",
                "Sanayi Bölgesi'ndeki bugünkü yerine taşınma. *" },
            ["The second generation takes over the management. *"] = new[] {
                "نسل دوم مدیریت را بر عهده می‌گیرد. *",
                "İkinci kuşak yönetimi devralır. *" },
            ["Modern test and calibration benches are installed for pumps and nozzles. *"] = new[] {
                "میزهای تست و کالیبراسیون مدرن برای پمپ‌ها و سوزن‌ها نصب می‌شوند. *",
                "Pompalar ve enjektörler için modern test ve kalibrasyon tezgâhları kurulur. *" },
            ["The workshop today, run by Hajj Samir Mohammed Al-Kermani and his sons."] = new[] {
                "کارگاه امروز، به مدیریت حاج سمیر محمد کرمانی و پسرانش.",
                "Bugünkü atölye; Hacı Samir Muhammed el-Kirmani ve oğulları tarafından yönetiliyor." }
        };

        private static string Fa(string en) { return en != null && ItemFaTr.TryGetValue(en, out var v) ? v[0] : ""; }
        private static string Tr(string en) { return en != null && ItemFaTr.TryGetValue(en, out var v) ? v[1] : ""; }

        // Fills only what is empty, so nothing staff have written is ever overwritten.
        private static bool Fill(Func<string> get, Action<string> set, string value)
        {
            if (!string.IsNullOrWhiteSpace(get()) || string.IsNullOrEmpty(value)) return false;
            set(value);
            return true;
        }

        private static bool FillDict(Dictionary<string, Dictionary<string, string>> target, string lang, Dictionary<string, string> defaults)
        {
            bool changed = false;
            if (!target.TryGetValue(lang, out var d) || d == null) { d = new Dictionary<string, string>(); target[lang] = d; changed = true; }
            foreach (var kv in defaults)
                if (!d.TryGetValue(kv.Key, out var cur) || string.IsNullOrWhiteSpace(cur)) { d[kv.Key] = kv.Value; changed = true; }
            return changed;
        }

        internal static bool AddFaTr(MapPin p)
        {
            bool c = false;
            c |= Fill(() => p.NameFa, v => p.NameFa = v, Fa(p.NameEn));
            c |= Fill(() => p.NameTr, v => p.NameTr = v, Tr(p.NameEn));
            c |= Fill(() => p.AddressFa, v => p.AddressFa = v, Fa(p.AddressEn));
            c |= Fill(() => p.AddressTr, v => p.AddressTr = v, Tr(p.AddressEn));
            return c;
        }

        internal static bool AddFaTr(ArchivalItem a)
        {
            bool c = false;
            c |= Fill(() => a.CaptionFa, v => a.CaptionFa = v, Fa(a.CaptionEn));
            c |= Fill(() => a.CaptionTr, v => a.CaptionTr = v, Tr(a.CaptionEn));
            return c;
        }

        internal static bool AddFaTr(ExplorerCard e)
        {
            bool c = false;
            c |= Fill(() => e.TitleFa, v => e.TitleFa = v, Fa(e.TitleEn));
            c |= Fill(() => e.TitleTr, v => e.TitleTr = v, Tr(e.TitleEn));
            c |= Fill(() => e.DescFa, v => e.DescFa = v, Fa(e.DescEn));
            c |= Fill(() => e.DescTr, v => e.DescTr = v, Tr(e.DescEn));
            return c;
        }

        internal static bool AddFaTr(GuideContentData g)
        {
            bool c = false;
            g.Translations ??= new Dictionary<string, Dictionary<string, string>>();
            c |= FillDict(g.Translations, "fa", GuideFa());
            c |= FillDict(g.Translations, "tr", GuideTr());
            foreach (var t in g.Topics ?? new List<GuideTopic>())
            {
                c |= Fill(() => t.TitleFa, v => t.TitleFa = v, Fa(t.TitleEn));
                c |= Fill(() => t.TitleTr, v => t.TitleTr = v, Tr(t.TitleEn));
                c |= Fill(() => t.BodyFa, v => t.BodyFa = v, Fa(t.BodyEn));
                c |= Fill(() => t.BodyTr, v => t.BodyTr = v, Tr(t.BodyEn));
            }
            foreach (var e in g.Equipment ?? new List<GuideEquipment>())
            {
                c |= Fill(() => e.NameFa, v => e.NameFa = v, Fa(e.NameEn));
                c |= Fill(() => e.NameTr, v => e.NameTr = v, Tr(e.NameEn));
                c |= Fill(() => e.SpecFa, v => e.SpecFa = v, Fa(e.SpecEn));
                c |= Fill(() => e.SpecTr, v => e.SpecTr = v, Tr(e.SpecEn));
                c |= Fill(() => e.UseFa, v => e.UseFa = v, Fa(e.UseEn));
                c |= Fill(() => e.UseTr, v => e.UseTr = v, Tr(e.UseEn));
            }
            return c;
        }

        internal static bool AddFaTr(IndustrialContentData d)
        {
            d.Translations ??= new Dictionary<string, Dictionary<string, string>>();
            bool c = FillDict(d.Translations, "fa", IndustrialFa());
            c |= FillDict(d.Translations, "tr", IndustrialTr());
            return c;
        }

        // Runs once at start-up, after the saved data is loaded.
        private static void EnsureFaTr()
        {
            try
            {
                lock (IndContentLock)
                    if (_indContent != null && AddFaTr(_indContent)) Persist(IndustrialContentPath, _indContent);
                lock (GuideLock)
                    if (_guideContent != null && AddFaTr(_guideContent)) Persist(GuideContentPath, _guideContent);

                bool pins = false;
                foreach (var p in _mapPins) pins |= AddFaTr(p);
                if (pins) Persist(MapPinsPath, _mapPins);

                bool album = false;
                foreach (var a in _archival) album |= AddFaTr(a);
                if (album) Persist(ArchivalPath, _archival);

                bool cards = false;
                foreach (var e in _explorerCards) cards |= AddFaTr(e);
                if (cards) Persist(ExplorerCardsPath, _explorerCards);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Persian/Turkish content step skipped: " + ex.Message);
            }
        }
    }
}

namespace ConsanWebApp
{
    public static partial class Pages
    {
        // The inner pages keep their own `translations` table in script. This adds Persian and
        // Turkish to it from the same dictionaries the admin edits (missing keys fall back to
        // Arabic for Persian and English for Turkish).
        internal static string FaTrScript(string page)
        {
            Dictionary<string, string> fa, tr;
            switch (page)
            {
                case "guide": fa = Store.GuideFa(); tr = Store.GuideTr(); break;
                case "industrial": fa = Store.IndustrialFa(); tr = Store.IndustrialTr(); break;
                default: fa = Store.IndustrialFa(); tr = Store.IndustrialTr(); break;
            }
            var site = Translations();
            foreach (var k in new[] { "contactTitle", "contactLabel1", "contactLabel2", "contactLabel3", "contactBadgeText" })
            {
                fa[k] = site["fa"].TryGetValue(k, out var f) ? f : "";
                tr[k] = site["tr"].TryGetValue(k, out var t) ? t : "";
            }
            fa["footerText"] = ""; fa["footerCopy"] = ""; tr["footerText"] = ""; tr["footerCopy"] = "";
            foreach (var kv in PageExtras(page)) { fa[kv.Key] = kv.Value[0]; tr[kv.Key] = kv.Value[1]; }
            return "translations.fa = Object.assign({}, translations.ar, " + System.Text.Json.JsonSerializer.Serialize(fa) + ");\n" +
                   "translations.tr = Object.assign({}, translations.en, " + System.Text.Json.JsonSerializer.Serialize(tr) + ");";
        }

        private static Dictionary<string, string[]> PageExtras(string page)
        {
            switch (page)
            {
                case "explorer":
                    return new Dictionary<string, string[]>
                    {
                        ["explorerTag"] = new[] { "کاوش کنید", "Keşfedin" },
                        ["explorerTitle"] = new[] { "کاوشگر <span class=\"highlight\">فنی</span>", "Teknik <span class=\"highlight\">Keşif</span>" },
                        ["explorerSub"] = new[] { "با جدیدترین فناوری‌ها، ابزارها و توانمندی‌های صنعتی کارگاه فرات میانه آشنا شوید", "Orta Fırat Atölyesi'ndeki en yeni teknolojileri, araçları ve endüstriyel yetenekleri keşfedin" },
                        ["learnMore"] = new[] { "مشاهده مشخصات ←", "Daha fazla bilgi →" }
                    };
                case "industrial":
                    return new Dictionary<string, string[]>
                    {
                        ["indSearchBtn"] = new[] { "جستجو", "Ara" },
                        ["workshopTag"] = new[] { "کارگاه", "Atölye" }
                    };
                case "login":
                    return new Dictionary<string, string[]>
                    {
                        ["pageTitleSignin"] = new[] { "ورود", "Giriş" },
                        ["pageTitleRegister"] = new[] { "ایجاد حساب جدید", "Hesap oluştur" },
                        ["pageSubSignin"] = new[] { "خوش آمدید، لطفاً اطلاعات خود را وارد کنید", "Tekrar hoş geldiniz, lütfen bilgilerinizi girin" },
                        ["pageSubRegister"] = new[] { "برای دسترسی به همه خدمات حساب بسازید", "Tüm hizmetlere erişmek için bir hesap oluşturun" },
                        ["tabSignin"] = new[] { "ورود", "Giriş yap" },
                        ["tabRegister"] = new[] { "ایجاد حساب", "Hesap oluştur" },
                        ["signinUserLabel"] = new[] { "نام کاربری یا ایمیل", "Kullanıcı adı veya e-posta" },
                        ["signinPassLabel"] = new[] { "رمز عبور", "Şifre" },
                        ["signinSubmitBtn"] = new[] { "ورود", "Giriş yap" },
                        ["regUserLabel"] = new[] { "نام کاربری", "Kullanıcı adı" },
                        ["regEmailLabel"] = new[] { "ایمیل", "E-posta" },
                        ["regPassLabel"] = new[] { "رمز عبور", "Şifre" },
                        ["regConfirmLabel"] = new[] { "تکرار رمز عبور", "Şifreyi onayla" },
                        ["regSubmitBtn"] = new[] { "ایجاد حساب", "Hesabı oluştur" },
                        ["backHomeLink"] = new[] { "← بازگشت به خانه", "← Ana sayfaya dön" },
                        ["demoAlertSignin"] = new[] { "با موفقیت وارد شدید", "Başarıyla giriş yapıldı" },
                        ["demoAlertRegister"] = new[] { "حساب با موفقیت ساخته شد", "Hesap başarıyla oluşturuldu" },
                        ["mismatch"] = new[] { "رمزهای عبور یکسان نیستند", "Şifreler eşleşmiyor" }
                    };
                default:
                    return new Dictionary<string, string[]>();
            }
        }
    }
}
