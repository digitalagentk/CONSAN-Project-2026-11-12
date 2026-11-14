// =============================================================================
// ME Workshop — service request form (/service)
// Every "request a service" button leads here. The customer fills in who they are,
// the vehicle and engine, the fault and its symptoms, urgency, how they will come
// and any questions. Sending saves a copy to the dashboard and opens WhatsApp to
// the first line with one clear, structured message (always in Arabic, for the
// workshop), ending with an open invitation for the workshop to ask questions.
// Desktop: the form beside a live preview of that message. Phone: one column, the
// preview folds away under the form.
// =============================================================================

namespace ConsanWebApp
{
    public static partial class Pages
    {
        public static string GetServiceRequestPage()
        {
            string body = """
            <body class="page-service">
                %%NAV%%
                <main class="sec svc-page">
                    <div class="container">
                        <header class="svc-head">
                            <h1 data-s="title">اطلب خدمة</h1>
                            <p class="lead" data-s="lead">املأ التفاصيل التالية ليصل طلبك إلى الورشة واضحاً وكاملاً، فيُرد عليك بسرعة ودقة. عند الإرسال يُفتح واتساب برسالة جاهزة.</p>
                        </header>

                        <div class="svc-layout">
                            <form class="svc-form" id="svcForm" novalidate>
                                <fieldset>
                                    <legend><span class="svc-step">1</span><span data-s="secYou">بياناتك</span></legend>
                                    <div class="svc-grid">
                                        <label class="svc-field"><span data-s="name">الاسم الكامل</span> <i>*</i>
                                            <input type="text" id="fName" autocomplete="name" required maxlength="80" /></label>
                                        <label class="svc-field"><span data-s="phone">رقم الهاتف</span> <i>*</i>
                                            <input type="tel" id="fPhone" autocomplete="tel" inputmode="tel" required maxlength="20" placeholder="07xx xxx xxxx" dir="ltr" /></label>
                                        <label class="svc-field"><span data-s="gov">المحافظة</span> <i>*</i>
                                            <select id="fGov" required></select></label>
                                        <label class="svc-field"><span data-s="when">أفضل وقت للتواصل</span>
                                            <select id="fWhen"></select></label>
                                    </div>
                                    <label class="svc-check"><input type="checkbox" id="fWaSame" checked /> <span data-s="waSame">رقم الواتساب هو نفس رقم الهاتف</span></label>
                                    <label class="svc-field" id="fWaWrap" hidden><span data-s="wa">رقم الواتساب</span>
                                        <input type="tel" id="fWa" inputmode="tel" maxlength="20" placeholder="07xx xxx xxxx" dir="ltr" /></label>
                                </fieldset>

                                <fieldset>
                                    <legend><span class="svc-step">2</span><span data-s="secService">الخدمة المطلوبة</span></legend>
                                    <div class="svc-grid">
                                        <label class="svc-field"><span data-s="service">نوع الخدمة</span> <i>*</i>
                                            <select id="fService" required></select></label>
                                        <label class="svc-field"><span data-s="arrive">كيف ستأتي إلى الورشة؟</span> <i>*</i>
                                            <select id="fArrive" required></select></label>
                                    </div>
                                    <div class="svc-radio" role="radiogroup" aria-labelledby="urgLbl">
                                        <span id="urgLbl" data-s="urgency">مدى الاستعجال</span>
                                        <label><input type="radio" name="urg" value="normal" checked /> <span data-s="urgNormal">عادي</span></label>
                                        <label><input type="radio" name="urg" value="urgent" /> <span data-s="urgUrgent">مستعجل — المركبة متوقفة عن العمل</span></label>
                                    </div>
                                </fieldset>

                                <fieldset>
                                    <legend><span class="svc-step">3</span><span data-s="secVehicle">المركبة والمحرك</span></legend>
                                    <div class="svc-grid">
                                        <label class="svc-field"><span data-s="vtype">نوع المركبة أو الآلة</span> <i>*</i>
                                            <select id="fVType" required></select></label>
                                        <label class="svc-field"><span data-s="model">الشركة والموديل والسنة</span> <i>*</i>
                                            <input type="text" id="fModel" required maxlength="80" data-ph="modelPh" placeholder="مثال: تويوتا هايلكس 2012" /></label>
                                        <label class="svc-field svc-wide"><span data-s="engine">نوع المحرك أو البمبة (إن كنت تعرفه)</span>
                                            <input type="text" id="fEngine" maxlength="100" data-ph="enginePh" placeholder="مثال: بمبة Bosch VE، كومن ريل Denso، محرك 2.5 لتر" /></label>
                                    </div>
                                </fieldset>

                                <fieldset>
                                    <legend><span class="svc-step">4</span><span data-s="secProblem">المشكلة</span></legend>
                                    <div class="svc-chips" id="fSymptoms" role="group" aria-labelledby="symLbl">
                                        <span id="symLbl" class="svc-chips-label" data-s="symptoms">الأعراض (اختر كل ما ينطبق)</span>
                                    </div>
                                    <label class="svc-field svc-wide"><span data-s="desc">صف المشكلة</span> <i>*</i>
                                        <textarea id="fDesc" required minlength="10" maxlength="1200" rows="4" data-ph="descPh" placeholder="ماذا يحدث بالضبط؟ متى بدأت المشكلة؟ هل تظهر عند التشغيل البارد أو بعد السخونة؟"></textarea></label>
                                    <label class="svc-field"><span data-s="prev">هل أُصلحت سابقاً؟</span>
                                        <select id="fPrev"></select></label>
                                </fieldset>

                                <fieldset>
                                    <legend><span class="svc-step">5</span><span data-s="secQuestions">أسئلتك للورشة</span></legend>
                                    <label class="svc-field svc-wide"><span data-s="questions">اكتب أي سؤال تريد أن تجيبك عنه الورشة (اختياري)</span>
                                        <textarea id="fQuestions" maxlength="800" rows="3" data-ph="questionsPh" placeholder="مثال: كم السعر التقريبي؟ كم يستغرق التصليح؟ هل القطعة متوفرة؟"></textarea></label>
                                </fieldset>

                                <p class="svc-error" id="svcError" role="alert" hidden></p>
                                <button type="submit" class="btn btn-brass svc-send">%%WAICON%%<span data-s="send">إرسال الطلب عبر واتساب</span></button>
                                <p class="svc-note" data-s="note">يُرسل الطلب إلى أبو مصطفى على الرقم 0780 974 4298. ستُحفظ نسخة منه لدى الورشة.</p>
                            </form>

                            <aside class="svc-preview" aria-live="polite">
                                <details id="svcPreviewBox" open>
                                    <summary data-s="preview">هكذا ستصل رسالتك إلى الورشة</summary>
                                    <pre id="svcPreview" dir="rtl" lang="ar"></pre>
                                </details>
                            </aside>
                        </div>

                        <div class="svc-done" id="svcDone" hidden>
                            <h2 data-s="doneTitle">تم تجهيز طلبك</h2>
                            <p data-s="doneBody">إذا لم يُفتح واتساب تلقائياً، اضغط الزر أدناه لإرسال الرسالة. ستتواصل معك الورشة قريباً.</p>
                            <div class="svc-done-actions">
                                <a class="btn btn-brass" id="svcDoneLink" href="#" target="_blank" rel="noopener">%%WAICON%%<span data-s="openWa">فتح واتساب وإرسال الطلب</span></a>
                                <button type="button" class="btn btn-line" id="svcAgain" data-s="again">طلب جديد</button>
                            </div>
                        </div>
                    </div>
                </main>
                %%FOOTER%%
                %%CONTACT%%
                <script>
                (function () {
                    'use strict';
                    var WA = '%%WA%%';
                    // UI text (the WhatsApp message itself is always Arabic, for the workshop)
                    var S = {
                        ar: {
                            title: 'اطلب خدمة', lead: 'املأ التفاصيل التالية ليصل طلبك إلى الورشة واضحاً وكاملاً، فيُرد عليك بسرعة ودقة. عند الإرسال يُفتح واتساب برسالة جاهزة.',
                            secYou: 'بياناتك', name: 'الاسم الكامل', phone: 'رقم الهاتف', gov: 'المحافظة', when: 'أفضل وقت للتواصل', waSame: 'رقم الواتساب هو نفس رقم الهاتف', wa: 'رقم الواتساب',
                            secService: 'الخدمة المطلوبة', service: 'نوع الخدمة', arrive: 'كيف ستأتي إلى الورشة؟', urgency: 'مدى الاستعجال', urgNormal: 'عادي', urgUrgent: 'مستعجل — المركبة متوقفة عن العمل',
                            secVehicle: 'المركبة والمحرك', vtype: 'نوع المركبة أو الآلة', model: 'الشركة والموديل والسنة', engine: 'نوع المحرك أو البمبة (إن كنت تعرفه)',
                            secProblem: 'المشكلة', symptoms: 'الأعراض (اختر كل ما ينطبق)', desc: 'صف المشكلة', prev: 'هل أُصلحت سابقاً؟',
                            secQuestions: 'أسئلتك للورشة', questions: 'اكتب أي سؤال تريد أن تجيبك عنه الورشة (اختياري)',
                            send: 'إرسال الطلب عبر واتساب', note: 'يُرسل الطلب إلى أبو مصطفى على الرقم 0780 974 4298. ستُحفظ نسخة منه لدى الورشة.', preview: 'هكذا ستصل رسالتك إلى الورشة',
                            doneTitle: 'تم تجهيز طلبك', doneBody: 'إذا لم يُفتح واتساب تلقائياً، اضغط الزر أدناه لإرسال الرسالة. ستتواصل معك الورشة قريباً.', openWa: 'فتح واتساب وإرسال الطلب', again: 'طلب جديد',
                            modelPh: 'مثال: تويوتا هايلكس 2012', enginePh: 'مثال: بمبة Bosch VE، كومن ريل Denso، محرك 2.5 لتر', descPh: 'ماذا يحدث بالضبط؟ متى بدأت المشكلة؟ هل تظهر عند التشغيل البارد أو بعد السخونة؟', questionsPh: 'مثال: كم السعر التقريبي؟ كم يستغرق التصليح؟ هل القطعة متوفرة؟',
                            choose: 'اختر…', errRequired: 'يرجى إكمال الحقول المطلوبة المعلّمة بـ *', errPhone: 'رقم الهاتف غير صحيح — اكتبه مثل 07701234567', errDesc: 'يرجى وصف المشكلة بجملة واحدة على الأقل'
                        },
                        en: {
                            title: 'Request a service', lead: 'Fill in the details below so your request reaches the workshop clear and complete, and you get a quick, precise answer. Sending opens WhatsApp with the message ready.',
                            secYou: 'Your details', name: 'Full name', phone: 'Phone number', gov: 'Governorate', when: 'Best time to contact you', waSame: 'My WhatsApp number is the same as my phone', wa: 'WhatsApp number',
                            secService: 'The service you need', service: 'Service type', arrive: 'How will you come to the workshop?', urgency: 'Urgency', urgNormal: 'Normal', urgUrgent: 'Urgent — the vehicle is stopped',
                            secVehicle: 'Vehicle and engine', vtype: 'Vehicle or machine type', model: 'Make, model and year', engine: 'Engine or pump type (if you know it)',
                            secProblem: 'The problem', symptoms: 'Symptoms (choose all that apply)', desc: 'Describe the problem', prev: 'Was it repaired before?',
                            secQuestions: 'Your questions for the workshop', questions: 'Any question you want the workshop to answer (optional)',
                            send: 'Send the request on WhatsApp', note: 'The request goes to Abu Mustafa on 0780 974 4298. A copy is kept by the workshop.', preview: 'This is how your message will reach the workshop',
                            doneTitle: 'Your request is ready', doneBody: 'If WhatsApp did not open by itself, press the button below to send the message. The workshop will get back to you soon.', openWa: 'Open WhatsApp and send', again: 'New request',
                            modelPh: 'e.g. Toyota Hilux 2012', enginePh: 'e.g. Bosch VE pump, Denso common rail, 2.5 L engine', descPh: 'What exactly happens? When did it start? Is it when cold, or after the engine warms up?', questionsPh: 'e.g. Roughly how much? How long will it take? Is the part available?',
                            choose: 'Choose…', errRequired: 'Please complete the required fields marked *', errPhone: 'The phone number looks wrong — write it like 07701234567', errDesc: 'Please describe the problem in at least one sentence'
                        },
                        ku: {
                            title: 'داوای خزمەتگوزاری', lead: 'وردەکارییەکانی خوارەوە پڕ بکەرەوە بۆ ئەوەی داواکارییەکەت بە ڕوونی و تەواوی بگاتە کارگە و بە خێرایی وەڵام بدرێتەوە. لە کاتی ناردندا واتساپ بە نامەیەکی ئامادەوە دەکرێتەوە.',
                            secYou: 'زانیارییەکانت', name: 'ناوی تەواو', phone: 'ژمارەی مۆبایل', gov: 'پارێزگا', when: 'باشترین کات بۆ پەیوەندی', waSame: 'ژمارەی واتساپ هەمان ژمارەی مۆبایلە', wa: 'ژمارەی واتساپ',
                            secService: 'خزمەتگوزاریی داواکراو', service: 'جۆری خزمەتگوزاری', arrive: 'چۆن دێیتە کارگە؟', urgency: 'پەلەکردن', urgNormal: 'ئاسایی', urgUrgent: 'بەپەلە — ئۆتۆمبێلەکە وەستاوە',
                            secVehicle: 'ئۆتۆمبێل و بزوێنەر', vtype: 'جۆری ئۆتۆمبێل یان ئامێر', model: 'کۆمپانیا، مۆدێل و ساڵ', engine: 'جۆری بزوێنەر یان پەمپ (ئەگەر دەیزانیت)',
                            secProblem: 'کێشەکە', symptoms: 'نیشانەکان (هەموو ئەوانەی دەگونجێن هەڵبژێرە)', desc: 'کێشەکە باس بکە', prev: 'پێشتر چاککراوەتەوە؟',
                            secQuestions: 'پرسیارەکانت بۆ کارگە', questions: 'هەر پرسیارێک کە دەتەوێت کارگە وەڵامی بداتەوە (ئارەزوومەندانە)',
                            send: 'ناردنی داواکاری لە ڕێگەی واتساپ', note: 'داواکارییەکە دەچێت بۆ ئەبو مستەفا لەسەر ژمارە 0780 974 4298. کۆپییەک لە کارگە هەڵدەگیرێت.', preview: 'نامەکەت بەم شێوەیە دەگاتە کارگە',
                            doneTitle: 'داواکارییەکەت ئامادەیە', doneBody: 'ئەگەر واتساپ خۆی نەکرایەوە، دوگمەی خوارەوە دابگرە بۆ ناردنی نامەکە. کارگە بەم زووانە پەیوەندیت پێوە دەکات.', openWa: 'کردنەوەی واتساپ و ناردن', again: 'داواکاریی نوێ',
                            modelPh: 'نموونە: تۆیۆتا هایلەکس 2012', enginePh: 'نموونە: پەمپی Bosch VE، کۆمن ڕەیڵی Denso، بزوێنەری 2.5 لیتر', descPh: 'بە وردی چی ڕوودەدات؟ کەی دەستی پێکرد؟ لە کاتی ساردیدایە یان دوای گەرمبوون؟', questionsPh: 'نموونە: نرخ نزیکەی چەندە؟ چاککردنەوە چەند دەخایەنێت؟ پارچەکە هەیە؟',
                            choose: 'هەڵبژێرە…', errRequired: 'تکایە خانە پێویستەکان پڕ بکەرەوە کە بە * نیشانکراون', errPhone: 'ژمارەی مۆبایل هەڵەیە — وەک 07701234567 بینووسە', errDesc: 'تکایە کێشەکە بە لانیکەم یەک ڕستە باس بکە'
                        }
                    };
                    // Options: [value written into the Arabic message, English label, Kurdish label]
                    var O = {
                        fGov: [['بغداد','Baghdad','بەغدا'],['البصرة','Basra','بەسرە'],['نينوى','Nineveh','نەینەوا'],['أربيل','Erbil','هەولێر'],['النجف','Najaf','نەجەف'],['كربلاء','Karbala','کەربەلا'],['بابل','Babylon','بابل'],['الأنبار','Anbar','ئەنبار'],['ديالى','Diyala','دیالە'],['ذي قار','Dhi Qar','زیقار'],['القادسية','Al-Qadisiyah','قادسیە'],['المثنى','Muthanna','موسەننا'],['ميسان','Maysan','مەیسان'],['واسط','Wasit','واسیت'],['صلاح الدين','Salah al-Din','سەلاحەدین'],['كركوك','Kirkuk','کەرکووک'],['دهوك','Duhok','دهۆک'],['السليمانية','Sulaymaniyah','سلێمانی'],['حلبجة','Halabja','هەڵەبجە'],['خارج العراق','Outside Iraq','دەرەوەی عێراق']],
                        fWhen: [['أي وقت','Any time','هەر کاتێک'],['صباحاً','Morning','بەیانی'],['ظهراً','Afternoon','نیوەڕۆ'],['مساءً','Evening','ئێوارە']],
                        fService: [['تصليح بمبة (مضخة) ديزل','Diesel pump repair','چاککردنەوەی پەمپی دیزڵ'],['تصليح أو تنظيف النوزلات والحاقنات','Nozzle / injector repair or cleaning','چاککردنەوە یان پاککردنەوەی نۆزڵ و ئینجێکتەر'],['كومن ريل (بمبة أو حاقنات)','Common rail (pump or injectors)','کۆمن ڕەیڵ (پەمپ یان ئینجێکتەر)'],['فحص ومعايرة على طاولة الفحص','Bench test and calibration','پشکنین و ڕێکخستن لەسەر مێزی پشکنین'],['فحص إلكتروني للأعطال','Electronic fault diagnosis','پشکنینی ئەلیکترۆنی'],['شراء بمبة أو نوزلات أو قطع','Buying a pump, nozzles or parts','کڕینی پەمپ، نۆزڵ یان پارچە'],['خدمة أخرى','Something else','خزمەتگوزارییەکی تر']],
                        fArrive: [['سأجلب المركبة إلى الورشة','I will bring the vehicle','ئۆتۆمبێلەکە دەهێنم'],['سأجلب البمبة أو النوزلات فقط','I will bring only the pump or nozzles','تەنها پەمپ یان نۆزڵەکان دەهێنم'],['أحتاج استشارة قبل الحضور','I need advice before coming','پێش هاتن ڕاوێژم پێویستە'],['لم أقرر بعد','Not decided yet','هێشتا بڕیارم نەداوە']],
                        fVType: [['سيارة صالون أو بيك أب','Car or pickup','ئۆتۆمبێلی سواری یان پیکاپ'],['شاحنة (لوري)','Truck','بارهەڵگر'],['باص','Bus','پاس'],['جرار أو آلة زراعية','Tractor / farm machine','تراکتۆر / ئامێری کشتوکاڵ'],['مولّدة كهرباء','Generator','مۆلیدە'],['آلية إنشائية ثقيلة','Heavy construction machine','ئامێری قورسی بیناسازی'],['زورق أو محرك بحري','Boat / marine engine','بەلەم / بزوێنەری دەریایی'],['أخرى','Other','هی تر']],
                        fPrev: [['لا، أول مرة','No, first time','نەخێر، یەکەمجارە'],['نعم، في ورشة أخرى','Yes, at another workshop','بەڵێ، لە کارگەیەکی تر'],['نعم، في ورشتكم سابقاً','Yes, at your workshop before','بەڵێ، پێشتر لە کارگەی ئێوە']]
                    };
                    var SYM = [['صعوبة في التشغيل','Hard starting','قورس دەکەوێتە کار'],['دخان أسود','Black smoke','دووکەڵی ڕەش'],['دخان أبيض','White smoke','دووکەڵی سپی'],['ضعف في العزم','Loss of power','لاوازیی هێز'],['استهلاك وقود عالٍ','High fuel use','بەکارهێنانی زۆری سووتەمەنی'],['طقطقة أو صوت غير طبيعي','Knocking or odd noise','دەنگی نائاسایی'],['تسريب وقود','Fuel leak','دزەکردنی سووتەمەنی'],['ينطفئ المحرك أثناء العمل','Engine cuts out','بزوێنەر دەکوژێتەوە'],['تقطيع عند التسارع','Hesitation when accelerating','پچڕپچڕ لە کاتی خێراکردن'],['لمبة المحرك مضاءة','Check-engine light on','گڵۆپی بزوێنەر داگیرساوە']];

                    var $ = function (id) { return document.getElementById(id); };
                    var form = $('svcForm');
                    function lang() { return (window.ME && window.ME.lang) || 'ar'; }
                    function idx() { var l = lang(); return l === 'en' ? 1 : (l === 'ku' ? 2 : 0); }

                    function fillSelects() {
                        var t = S[lang()];
                        Object.keys(O).forEach(function (id) {
                            var sel = $(id), keep = sel.value;
                            sel.innerHTML = '';
                            if (id !== 'fWhen' && id !== 'fPrev') {
                                var ph = document.createElement('option'); ph.value = ''; ph.textContent = t.choose; sel.appendChild(ph);
                            }
                            O[id].forEach(function (o) {
                                var op = document.createElement('option'); op.value = o[0]; op.textContent = o[idx()]; sel.appendChild(op);
                            });
                            sel.value = keep;
                            if (sel.selectedIndex < 0) sel.selectedIndex = 0; // no saved choice: placeholder, or the first option
                        });
                    }
                    function fillChips() {
                        var box = $('fSymptoms');
                        var checked = Array.prototype.map.call(box.querySelectorAll('input:checked'), function (c) { return c.value; });
                        box.querySelectorAll('label').forEach(function (l) { l.remove(); });
                        SYM.forEach(function (s) {
                            var l = document.createElement('label');
                            l.className = 'svc-chip';
                            l.innerHTML = '<input type="checkbox" /><span></span>';
                            l.firstChild.value = s[0];
                            l.firstChild.checked = checked.indexOf(s[0]) !== -1;
                            l.lastChild.textContent = s[idx()];
                            box.appendChild(l);
                        });
                    }
                    function paint() {
                        var t = S[lang()];
                        document.querySelectorAll('[data-s]').forEach(function (el) { if (t[el.getAttribute('data-s')]) el.textContent = t[el.getAttribute('data-s')]; });
                        document.querySelectorAll('[data-ph]').forEach(function (el) { el.placeholder = t[el.getAttribute('data-ph')]; });
                        fillSelects(); fillChips(); preview();
                    }

                    function digits(s) { return String(s || '').replace(/[^\d+]/g, ''); }
                    function validPhone(s) { var d = digits(s).replace(/^\+/, ''); return /^(07\d{9}|9647\d{9}|\d{9,15})$/.test(d); }
                    function val(id) { return $(id).value.trim(); }
                    function urgency() { var r = form.querySelector('input[name="urg"]:checked'); return r && r.value === 'urgent'; }
                    function symptoms() { return Array.prototype.map.call($('fSymptoms').querySelectorAll('input:checked'), function (c) { return c.value; }); }

                    var reqId = '';
                    function newId() {
                        var d = new Date(), p = function (n) { return (n < 10 ? '0' : '') + n; };
                        reqId = 'ME-' + p(d.getMonth() + 1) + p(d.getDate()) + '-' + p(d.getHours()) + p(d.getMinutes()) + '-' + Math.floor(100 + Math.random() * 900);
                    }

                    // The message the workshop receives: grouped, labelled, nothing to guess.
                    function message() {
                        var dash = '—';
                        var or = function (v) { return v ? v : dash; };
                        var waLine = $('fWaSame').checked ? 'نفس رقم الهاتف' : or(val('fWa'));
                        var syms = symptoms();
                        var q = val('fQuestions');
                        var L = [];
                        L.push('*طلب خدمة — ورشة الفرات الأوسط (ME Workshop)*');
                        L.push('رقم الطلب: ' + reqId);
                        if (urgency()) L.push('*تنبيه: طلب مستعجل — المركبة متوقفة عن العمل*');
                        L.push('');
                        L.push('*1) بيانات الزبون*');
                        L.push('• الاسم: ' + or(val('fName')));
                        L.push('• الهاتف: ' + or(val('fPhone')));
                        L.push('• واتساب: ' + waLine);
                        L.push('• المحافظة: ' + or(val('fGov')));
                        L.push('• أفضل وقت للتواصل: ' + or(val('fWhen')));
                        L.push('');
                        L.push('*2) الخدمة المطلوبة*');
                        L.push('• نوع الخدمة: ' + or(val('fService')));
                        L.push('• الحضور: ' + or(val('fArrive')));
                        L.push('• الاستعجال: ' + (urgency() ? 'مستعجل' : 'عادي'));
                        L.push('');
                        L.push('*3) المركبة والمحرك*');
                        L.push('• النوع: ' + or(val('fVType')));
                        L.push('• الشركة والموديل والسنة: ' + or(val('fModel')));
                        L.push('• المحرك / البمبة: ' + or(val('fEngine')));
                        L.push('');
                        L.push('*4) المشكلة*');
                        L.push('• الأعراض: ' + (syms.length ? syms.join('، ') : dash));
                        L.push('• الوصف: ' + or(val('fDesc')));
                        L.push('• تصليح سابق: ' + or(val('fPrev')));
                        L.push('');
                        L.push('*5) أسئلتي للورشة*');
                        L.push(q ? q : 'لا توجد أسئلة حالياً.');
                        L.push('');
                        L.push('—');
                        L.push('إذا احتاجت الورشة أي معلومة إضافية أو لديها سؤال لي، فأنا جاهز للرد هنا.');
                        return L.join('\n');
                    }
                    function preview() { $('svcPreview').textContent = message(); }

                    function fail(msg, el) {
                        var e = $('svcError'); e.textContent = msg; e.hidden = false;
                        if (el) { el.focus(); el.closest('.svc-field') && el.closest('.svc-field').classList.add('svc-bad'); }
                    }

                    form.addEventListener('input', function (e) {
                        var f = e.target.closest('.svc-field'); if (f) f.classList.remove('svc-bad');
                        $('svcError').hidden = true; preview();
                    });
                    form.addEventListener('change', preview);
                    $('fWaSame').addEventListener('change', function () { $('fWaWrap').hidden = this.checked; preview(); });

                    form.addEventListener('submit', function (e) {
                        e.preventDefault();
                        var t = S[lang()];
                        var req = ['fName', 'fPhone', 'fGov', 'fService', 'fArrive', 'fVType', 'fModel', 'fDesc'];
                        for (var i = 0; i < req.length; i++) { if (!val(req[i])) return fail(t.errRequired, $(req[i])); }
                        if (!validPhone(val('fPhone'))) return fail(t.errPhone, $('fPhone'));
                        if (!$('fWaSame').checked && val('fWa') && !validPhone(val('fWa'))) return fail(t.errPhone, $('fWa'));
                        if (val('fDesc').length < 10) return fail(t.errDesc, $('fDesc'));

                        var text = message();
                        var url = 'https://wa.me/' + WA + '?text=' + encodeURIComponent(text);
                        // copy for the dashboard (keepalive lets it finish while WhatsApp opens)
                        try {
                            fetch('/api/service-requests', {
                                method: 'POST', headers: { 'Content-Type': 'application/json' }, keepalive: true,
                                body: JSON.stringify({ fullName: val('fName'), phone: val('fPhone'), serviceType: val('fService'), message: text })
                            }).catch(function () { });
                        } catch (err) { }
                        var win = window.open(url, '_blank');
                        if (win) { try { win.opener = null; } catch (err) { } }
                        $('svcDoneLink').href = url;
                        document.querySelector('.svc-layout').hidden = true;
                        $('svcDone').hidden = false;
                        $('svcDone').scrollIntoView({ behavior: 'smooth', block: 'center' });
                        if (!win) window.location.href = url;
                    });

                    $('svcAgain').addEventListener('click', function () {
                        form.reset(); $('fWaWrap').hidden = true; newId(); paint();
                        $('svcDone').hidden = true; document.querySelector('.svc-layout').hidden = false;
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    });

                    // phone: the preview starts folded; desktop: open beside the form
                    if (window.matchMedia('(max-width: 900px)').matches) $('svcPreviewBox').open = false;

                    newId();
                    paint();
                    document.addEventListener('me:lang', paint);
                })();
                </script>
            </body>
            """;

            body = body.Replace("%%NAV%%", GetNavbarHtml())
                       .Replace("%%FOOTER%%", GetFooterHtml())
                       .Replace("%%CONTACT%%", GetContactWidgetHtml())
                       .Replace("%%WA%%", PrimaryWhatsApp)
                       .Replace("%%WAICON%%", WhatsAppSvg.Replace("<svg ", "<svg fill=\"currentColor\" "));

            return PageShell("اطلب خدمة | ورشة الفرات الأوسط — ME Workshop", "", body);
        }
    }
}
