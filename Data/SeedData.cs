using System;
using System.Collections.Generic;
using System.Linq;
using ConsanWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsanWebApp.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            // 1. Admin Keys — مفتاح افتراضي
            if (!db.AdminKeys.Any())
            {
                db.AdminKeys.Add(new AdminKey
                {
                    KeyHash = Hash("admin"),
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    IsActive = true
                });
                db.SaveChanges();
                Console.WriteLine("✅ Seeded AdminKeys");
            }

            // 2. Hero Images
            if (!db.HeroImages.Any())
            {
                var images = new[]
                {
                    "https://images.unsplash.com/photo-1581093588401-fbb62a02f120?w=1920&h=1080&fit=crop",
                    "https://images.unsplash.com/photo-1581092335876-1b5a6b5b0b1f?w=1920&h=1080&fit=crop",
                    "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?w=1920&h=1080&fit=crop",
                    "https://images.unsplash.com/photo-1504328345606-18bbc8c9d7d1?w=1920&h=1080&fit=crop"
                };
                for (int i = 0; i < images.Length; i++)
                {
                    db.HeroImages.Add(new HeroImage { ImageUrl = images[i], SortOrder = i });
                }
                db.SaveChanges();
                Console.WriteLine("✅ Seeded HeroImages");
            }

            // 3. Map Pins (3 defaults)
            if (!db.MapPins.Any())
            {
                db.MapPins.AddRange(
                    new MapPin
                    {
                        NameAr = "قسم الاستقبال والإدارة",
                        NameEn = "Reception & Management",
                        NameKu = "بەشی وەرگرتن و بەڕێوەبردن",
                        AddressAr = "النجف الأشرف - الحي الصناعي",
                        AddressEn = "Najaf Al-Ashraf - Industrial District",
                        AddressKu = "نەجەف ئەشرەف - گەڕەکی پیشەسازی",
                        Phone = "07809744298",
                        Latitude = 32.0000,
                        Longitude = 44.3300,
                        IsApproved = true
                    },
                    new MapPin
                    {
                        NameAr = "قسم تصليح البمبات",
                        NameEn = "Pump Repair Department",
                        NameKu = "بەشی چاککردنەوەی پەمپ",
                        AddressAr = "النجف الأشرف - الحي الصناعي",
                        AddressEn = "Najaf Al-Ashraf - Industrial District",
                        AddressKu = "نەجەف ئەشرەف - گەڕەکی پیشەسازی",
                        Phone = "07805125702",
                        Latitude = 32.0006,
                        Longitude = 44.3308,
                        IsApproved = true
                    },
                    new MapPin
                    {
                        NameAr = "قسم النوزلات والحاقنات",
                        NameEn = "Nozzles & Injectors Department",
                        NameKu = "بەشی نۆزل و ئینجێکتۆر",
                        AddressAr = "النجف الأشرف - الحي الصناعي",
                        AddressEn = "Najaf Al-Ashraf - Industrial District",
                        AddressKu = "نەجەف ئەشرەف - گەڕەکی پیشەسازی",
                        Phone = "07801763759",
                        Latitude = 31.9994,
                        Longitude = 44.3294,
                        IsApproved = true
                    }
                );
                db.SaveChanges();
                Console.WriteLine("✅ Seeded MapPins");
            }

            // 4. Industrial Settings
            if (!db.IndustrialSettings.Any())
            {
                db.IndustrialSettings.Add(new IndustrialSetting
                {
                    MapEmbedUrl = "https://www.google.com/maps?q=Industrial+Zone,+Najaf+Al-Ashraf,+Iraq&output=embed"
                });
                db.SaveChanges();
                Console.WriteLine("✅ Seeded IndustrialSettings");
            }

            // 5. Guide Topics
            if (!db.GuideTopics.Any())
            {
                db.GuideTopics.AddRange(
                    new GuideTopic
                    {
                        Icon = "🏭",
                        TitleAr = "أقسام الورشة",
                        TitleEn = "Workshop Departments",
                        TitleKu = "بەشەکانی کارگە",
                        BodyAr = "يضم القسم أقساماً متخصصة: قسم تصليح البمبات، قسم النوزلات والحاقنات، قسم الفحص الإلكتروني، وقسم الخراطة الصناعية.",
                        BodyEn = "The workshop includes specialized departments: Pump Repair, Nozzles & Injectors, Electronic Diagnostics, and Industrial Lathe.",
                        BodyKu = "کارگەکە بەشە تایبەتەکانی تێدایە: بەشی چاککردنەوەی پەمپ، بەشی نۆزل و ئینجێکتۆر، بەشی پشکنینی ئەلیکترۆنی، و بەشی خەرادەی پیشەسازی.",
                        SortOrder = 1
                    },
                    new GuideTopic
                    {
                        Icon = "🛡️",
                        TitleAr = "معايير السلامة المهنية",
                        TitleEn = "Occupational Safety Standards",
                        TitleKu = "ستانداردەکانی سەلامەتی پیشەیی",
                        BodyAr = "نلتزم بأعلى معايير السلامة: معدات الحماية الشخصية، طفايات الحريق، لافتات التحذير، وخطة الطوارئ.",
                        BodyEn = "We adhere to the highest safety standards: personal protective equipment, fire extinguishers, warning signs, and emergency plans.",
                        BodyKu = "پابەندین بە بەرزترین ستانداردەکانی سەلامەتی.",
                        SortOrder = 2
                    },
                    new GuideTopic
                    {
                        Icon = "🔄",
                        TitleAr = "سير العمل والخدمات",
                        TitleEn = "Service Workflow",
                        TitleKu = "پرۆسەی کار و خزمەتگوزاری",
                        BodyAr = "نتبع خطوات دقيقة: استقبال الطلب، الفحص والتشخيص، تقدير التكلفة، تنفيذ العمل، اختبار الجودة، ثم التسليم والمتابعة.",
                        BodyEn = "We follow precise steps: request reception, inspection and diagnosis, cost estimation, work execution, quality testing, then delivery and follow-up.",
                        BodyKu = "شوێنپێی هەنگاوە وردەکان دەکەوین.",
                        SortOrder = 3
                    }
                );
                db.SaveChanges();
                Console.WriteLine("✅ Seeded GuideTopics");
            }

            // 6. Guide Equipment
            if (!db.GuideEquipment.Any())
            {
                db.GuideEquipment.AddRange(
                    new GuideEquipment
                    {
                        NameAr = "ماكينة اختبار النوزلات",
                        NameEn = "Nozzle Test Machine",
                        NameKu = "ئامێری تاقیکردنەوەی نۆزل",
                        SpecAr = "ضغط حتى 3000 بار، تحكم رقمي",
                        SpecEn = "Pressure up to 3000 bar, digital control",
                        SpecKu = "پەستان تا 3000 بار",
                        UseAr = "اختبار وضبط رشاشات الديزل",
                        UseEn = "Testing and calibrating diesel injectors",
                        UseKu = "تاقیکردنەوە و ڕێکخستنی ئینجێکتۆری دیزل",
                        SortOrder = 1
                    },
                    new GuideEquipment
                    {
                        NameAr = "مخرطة CNC",
                        NameEn = "CNC Lathe",
                        NameKu = "خەرادەی CNC",
                        SpecAr = "قطر 500 مم، دقة 0.01 مم",
                        SpecEn = "Diameter 500 mm, precision 0.01 mm",
                        SpecKu = "تیرە 500 مم",
                        UseAr = "تصنيع قطع الغيار الدقيقة",
                        UseEn = "Manufacturing precision spare parts",
                        UseKu = "دروستکردنی پارچەی یەدەگی ورد",
                        SortOrder = 2
                    }
                );
                db.SaveChanges();
                Console.WriteLine("✅ Seeded GuideEquipment");
            }

            // 7. SiteContentTranslations (مفاتيح عربية أساسية)
            if (!db.SiteContentTranslations.Any())
            {
                var arDefaults = new Dictionary<string, string>
                {
                    ["navHome"] = "الرئيسية",
                    ["navAbout"] = "من نحن",
                    ["navServices"] = "خدماتنا",
                    ["navIndustrial"] = "خدمات صناعية",
                    ["navExplorer"] = "المستكشف",
                    ["navGuide"] = "الدليل الشامل",
                    ["heroTitle"] = "<span class=\"highlight\">ورشة الفرات الأوسط</span>",
                    ["heroSubtitle"] = "لتصليح وبيع كافة أنواع <span class=\"highlight\">البمبات والنوزلات</span>",
                    ["aboutTag"] = "من نحن",
                    ["aboutTitle"] = "نبذة عن <span class=\"highlight\">مؤسستنا</span>",
                    ["servicesTitle"] = "ما <span class=\"highlight\">نقدمه</span>",
                    ["footerCopy"] = "© ورشة الفرات الأوسط"
                };
                foreach (var kv in arDefaults)
                {
                    db.SiteContentTranslations.Add(new SiteContentTranslation
                    {
                        Lang = "ar",
                        Key = kv.Key,
                        Value = kv.Value
                    });
                }
                db.SaveChanges();
                Console.WriteLine("✅ Seeded SiteContentTranslations (ar)");
            }

            // 8. Industrial Content Translations
            if (!db.IndustrialContentTranslations.Any())
            {
                var arInd = new Dictionary<string, string>
                {
                    ["indVisitTag"] = "زوروا موقعنا",
                    ["indVisitTitle"] = "موقعنا في <span class=\"highlight\">الحي الصناعي بالنجف الأشرف</span>",
                    ["indSearchPlaceholder"] = "ابحث عن الاسم، العنوان، أو رقم الهاتف...",
                    ["indHoursNote"] = "ساعات العمل: يومياً من السبت إلى الخميس، 8 صباحاً حتى 6 مساءً",
                    ["addLocationBtn"] = "إضافة موقع جديد على الخريطة"
                };
                foreach (var kv in arInd)
                {
                    db.IndustrialContentTranslations.Add(new IndustrialContentTranslation
                    {
                        Lang = "ar",
                        Key = kv.Key,
                        Value = kv.Value
                    });
                }
                db.SaveChanges();
                Console.WriteLine("✅ Seeded IndustrialContentTranslations (ar)");
            }

            // =========================================================
            // 9. NEW: Seed Default Admin Account
            // =========================================================
            try
            {
                if (!db.Users.Any(u => u.Role == "Admin"))
                {
                    var adminEmail = "admin@alfurat.com";
                    var adminPassword = "Admin@123456"; // ⚠️ غيّرها فوراً بعد أول تسجيل دخول!

                    if (!db.Users.Any(u => u.Email.ToLower() == adminEmail.ToLower()))
                    {
                        db.Users.Add(new User
                        {
                            FullName = "مدير النظام",
                            Username = adminEmail, // للتوافق مع unique index
                            Email = adminEmail,
                            PasswordHash = Hash(adminPassword),
                            Role = "Admin",
                            IsVerified = true,
                            Status = "Active",
                            RegisteredAt = DateTime.UtcNow.ToString("o"),
                            LastLoginAt = ""
                        });
                        db.SaveChanges();
                        Console.WriteLine("========================================");
                        Console.WriteLine("✅ Seeded Default Admin Account:");
                        Console.WriteLine($"   Email:    {adminEmail}");
                        Console.WriteLine($"   Password: {adminPassword}");
                        Console.WriteLine("   ⚠️  غيّر كلمة المرور فوراً بعد أول تسجيل دخول!");
                        Console.WriteLine("========================================");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Admin seed skipped: {ex.Message}");
            }

            Console.WriteLine("========================================");
            Console.WriteLine("✅ Seed Data Complete");
            Console.WriteLine("========================================");
        }

        private static string Hash(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input ?? ""));
                var sb = new System.Text.StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}