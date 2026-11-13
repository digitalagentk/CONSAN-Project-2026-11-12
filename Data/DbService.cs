using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsanWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsanWebApp.Data
{
    /// <summary>
    /// بديل Store — كل البيانات من/إلى SQL Server
    /// </summary>
    public static class DbService
    {
        // =========================================================
        // User
        // =========================================================
        public static List<User> GetUsers(AppDbContext db) =>
            db.Users.OrderByDescending(u => u.RegisteredAt).ToList();

        public static (bool ok, string error) Register(AppDbContext db, string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "اسم المستخدم وكلمة المرور مطلوبان");

            if (db.Users.Any(u => u.Username == username))
                return (false, "اسم المستخدم مستخدم مسبقاً");

            if (!string.IsNullOrWhiteSpace(email) && db.Users.Any(u => u.Email.ToLower() == email.ToLower()))
                return (false, "البريد الإلكتروني مستخدم مسبقاً");

            db.Users.Add(new User
            {
                FullName = username,
                Username = username,
                Email = email ?? "",
                PasswordHash = Hash(password),
                Role = "Manager",
                IsVerified = false,
                Status = "Active",
                RegisteredAt = DateTime.UtcNow.ToString("o"),
                LastLoginAt = ""
            });
            db.SaveChanges();
            return (true, "");
        }

        public static bool Login(AppDbContext db, string username, string password)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return false;
            if (user.PasswordHash != Hash(password)) return false;
            user.LastLoginAt = DateTime.UtcNow.ToString("o");
            db.SaveChanges();
            return true;
        }

        public static bool DeleteUser(AppDbContext db, int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return false;
            db.Users.Remove(user);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // NEW: Account Management (Admin/Manager) by Email
        // =========================================================

        /// <summary>
        /// تسجيل الدخول بالإيميل — يُرجع المستخدم إذا نجح، null إذا فشل
        /// </summary>
        public static User LoginByEmail(AppDbContext db, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = db.Users.FirstOrDefault(u =>
                u.Email.ToLower() == email.ToLower() &&
                u.Status == "Active");

            if (user == null) return null;
            if (user.PasswordHash != Hash(password)) return null;

            user.LastLoginAt = DateTime.UtcNow.ToString("o");
            db.SaveChanges();
            return user;
        }

        /// <summary>
        /// جلب كل الحسابات (Admin + Manager) — للعرض في الجدول
        /// </summary>
        public static List<AccountListDto> GetAccountsForList(AppDbContext db)
        {
            return db.Users
                .OrderByDescending(u => u.RegisteredAt)
                .Select(u => new AccountListDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    IsVerified = u.IsVerified,
                    CreatedAt = u.RegisteredAt
                })
                .ToList();
        }

        /// <summary>
        /// إنشاء حساب جديد (من لوحة التحكم)
        /// </summary>
        public static (bool ok, string error, User created) CreateAccount(
            AppDbContext db, CreateAccountDto dto)
        {
            if (dto == null)
                return (false, "بيانات غير صالحة", null);

            if (string.IsNullOrWhiteSpace(dto.FullName))
                return (false, "الاسم الكامل مطلوب", null);

            if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email))
                return (false, "بريد إلكتروني غير صالح", null);

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return (false, "كلمة المرور يجب أن تكون 6 أحرف على الأقل", null);

            if (dto.Role != "Admin" && dto.Role != "Manager")
                return (false, "الدور يجب أن يكون Admin أو Manager", null);

            // لا يمكن تكرار الإيميل
            bool exists = db.Users.Any(u => u.Email.ToLower() == dto.Email.ToLower());
            if (exists)
                return (false, "هذا البريد الإلكتروني مستخدم مسبقاً", null);

            // Username فريد أيضاً — نستخدم الإيميل كـ username
            if (db.Users.Any(u => u.Username.ToLower() == dto.Email.ToLower()))
                return (false, "هذا البريد الإلكتروني مستخدم مسبقاً", null);

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Username = dto.Email.Trim(), // للتوافق مع الـ unique index القديم
                Email = dto.Email.Trim(),
                PasswordHash = Hash(dto.Password),
                Role = dto.Role,
                IsVerified = true, // الأدمن ينشئه فيكون موثّقاً مباشرة
                Status = "Active",
                RegisteredAt = DateTime.UtcNow.ToString("o"),
                LastLoginAt = ""
            };

            db.Users.Add(user);
            db.SaveChanges();
            return (true, "", user);
        }

        /// <summary>
        /// تغيير كلمة مرور حساب (Admin فقط)
        /// </summary>
        public static (bool ok, string error) ChangeUserPassword(
            AppDbContext db, int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "كلمة المرور يجب أن تكون 6 أحرف على الأقل");

            var user = db.Users.Find(userId);
            if (user == null)
                return (false, "الحساب غير موجود");

            user.PasswordHash = Hash(newPassword);
            db.SaveChanges();
            return (true, "");
        }

        /// <summary>
        /// حذف حساب (Admin فقط — ولا يمكنه حذف نفسه)
        /// </summary>
        public static (bool ok, string error) DeleteAccount(
            AppDbContext db, int userId, int currentAdminId)
        {
            if (userId == currentAdminId)
                return (false, "لا يمكنك حذف حسابك الخاص");

            var user = db.Users.Find(userId);
            if (user == null)
                return (false, "الحساب غير موجود");

            db.Users.Remove(user);
            db.SaveChanges();
            return (true, "");
        }

        /// <summary>
        /// التحقق من صحة صيغة الإيميل
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // ServiceRequest
        // =========================================================
        public static List<ServiceRequest> GetRequests(AppDbContext db) =>
            db.ServiceRequests.OrderByDescending(r => r.SubmittedAt).ToList();

        public static ServiceRequest AddRequest(AppDbContext db, string fullName, string phone, string serviceType, string message)
        {
            var item = new ServiceRequest
            {
                FullName = fullName ?? "",
                Phone = phone ?? "",
                ServiceType = serviceType ?? "",
                Message = message ?? "",
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow.ToString("o")
            };
            db.ServiceRequests.Add(item);
            db.SaveChanges();
            return item;
        }

        public static bool UpdateRequestStatus(AppDbContext db, int id, string status)
        {
            var req = db.ServiceRequests.Find(id);
            if (req == null) return false;
            req.Status = status;
            db.SaveChanges();
            return true;
        }

        public static bool DeleteRequest(AppDbContext db, int id)
        {
            var req = db.ServiceRequests.Find(id);
            if (req == null) return false;
            db.ServiceRequests.Remove(req);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // MapPin
        // =========================================================
        public static List<MapPin> GetApprovedMapPins(AppDbContext db) =>
            db.MapPins.Where(p => p.IsApproved).ToList();

        public static List<MapPin> GetAllMapPins(AppDbContext db) =>
            db.MapPins.ToList();

        public static List<MapPin> GetPendingMapPins(AppDbContext db) =>
            db.MapPins.Where(p => !p.IsApproved).OrderByDescending(p => p.SubmittedAt).ToList();

        public static MapPin AddMapPin(AppDbContext db, MapPin pin)
        {
            pin.Id = 0; // auto-increment
            db.MapPins.Add(pin);
            db.SaveChanges();
            return pin;
        }

        public static bool UpdateMapPin(AppDbContext db, int id, MapPin updated)
        {
            var existing = db.MapPins.Find(id);
            if (existing == null) return false;
            existing.NameAr = updated.NameAr ?? "";
            existing.NameEn = updated.NameEn ?? "";
            existing.NameKu = updated.NameKu ?? "";
            existing.AddressAr = updated.AddressAr ?? "";
            existing.AddressEn = updated.AddressEn ?? "";
            existing.AddressKu = updated.AddressKu ?? "";
            existing.Phone = updated.Phone ?? "";
            existing.Latitude = updated.Latitude;
            existing.Longitude = updated.Longitude;
            existing.MapEmbedUrl = updated.MapEmbedUrl ?? "";
            db.SaveChanges();
            return true;
        }

        public static bool ApproveMapPin(AppDbContext db, int id)
        {
            var pin = db.MapPins.Find(id);
            if (pin == null) return false;
            pin.IsApproved = true;
            db.SaveChanges();
            return true;
        }

        public static bool DeleteMapPin(AppDbContext db, int id)
        {
            var pin = db.MapPins.Find(id);
            if (pin == null) return false;
            db.MapPins.Remove(pin);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // ArchivalItem
        // =========================================================
        public static List<ArchivalItem> GetArchivalAlbum(AppDbContext db) =>
            db.ArchivalItems.OrderByDescending(a => a.Year).ToList();

        public static ArchivalItem AddArchivalItem(AppDbContext db, ArchivalItem item)
        {
            item.Id = 0;
            item.CreatedAt = DateTime.UtcNow.ToString("o");
            db.ArchivalItems.Add(item);
            db.SaveChanges();
            return item;
        }

        public static bool UpdateArchivalItem(AppDbContext db, int id, ArchivalItem updated)
        {
            var existing = db.ArchivalItems.Find(id);
            if (existing == null) return false;
            existing.Year = updated.Year ?? "";
            existing.ImageUrl = updated.ImageUrl ?? "";
            existing.CaptionAr = updated.CaptionAr ?? "";
            existing.CaptionEn = updated.CaptionEn ?? "";
            existing.CaptionKu = updated.CaptionKu ?? "";
            db.SaveChanges();
            return true;
        }

        public static bool DeleteArchivalItem(AppDbContext db, int id)
        {
            var item = db.ArchivalItems.Find(id);
            if (item == null) return false;
            db.ArchivalItems.Remove(item);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // ExplorerCard
        // =========================================================
        public static List<ExplorerCard> GetExplorerCards(AppDbContext db) =>
            db.ExplorerCards.OrderBy(c => c.SortOrder).ToList();

        public static ExplorerCard AddExplorerCard(AppDbContext db, ExplorerCard card)
        {
            card.Id = 0;
            card.CreatedAt = DateTime.UtcNow.ToString("o");
            db.ExplorerCards.Add(card);
            db.SaveChanges();
            return card;
        }

        public static bool UpdateExplorerCard(AppDbContext db, int id, ExplorerCard updated)
        {
            var existing = db.ExplorerCards.Find(id);
            if (existing == null) return false;
            existing.Icon = updated.Icon;
            existing.ImageUrl = updated.ImageUrl;
            existing.TitleAr = updated.TitleAr;
            existing.TitleEn = updated.TitleEn;
            existing.TitleKu = updated.TitleKu;
            existing.DescAr = updated.DescAr;
            existing.DescEn = updated.DescEn;
            existing.DescKu = updated.DescKu;
            existing.Tags = updated.Tags;
            existing.Link = updated.Link;
            existing.SortOrder = updated.SortOrder;
            db.SaveChanges();
            return true;
        }

        public static bool DeleteExplorerCard(AppDbContext db, int id)
        {
            var card = db.ExplorerCards.Find(id);
            if (card == null) return false;
            db.ExplorerCards.Remove(card);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // SiteContent
        // =========================================================
        public static Dictionary<string, Dictionary<string, string>> GetSiteContent(AppDbContext db)
        {
            var result = new Dictionary<string, Dictionary<string, string>>
            {
                ["ar"] = new Dictionary<string, string>(),
                ["en"] = new Dictionary<string, string>(),
                ["ku"] = new Dictionary<string, string>()
            };
            foreach (var t in db.SiteContentTranslations.ToList())
            {
                if (!result.ContainsKey(t.Lang))
                    result[t.Lang] = new Dictionary<string, string>();
                result[t.Lang][t.Key] = t.Value;
            }
            return result;
        }

        public static void SaveSiteContent(AppDbContext db, Dictionary<string, Dictionary<string, string>> data)
        {
            // احذف كل القديم وأضف الجديد
            db.SiteContentTranslations.RemoveRange(db.SiteContentTranslations);
            db.SaveChanges();

            foreach (var lang in data.Keys)
            {
                foreach (var kv in data[lang])
                {
                    db.SiteContentTranslations.Add(new SiteContentTranslation
                    {
                        Lang = lang,
                        Key = kv.Key,
                        Value = kv.Value ?? ""
                    });
                }
            }
            db.SaveChanges();
        }

        public static List<HeroImage> GetHeroImages(AppDbContext db) =>
            db.HeroImages.OrderBy(h => h.SortOrder).ToList();

        public static void SaveHeroImages(AppDbContext db, List<string> urls)
        {
            db.HeroImages.RemoveRange(db.HeroImages);
            db.SaveChanges();
            for (int i = 0; i < urls.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(urls[i]))
                {
                    db.HeroImages.Add(new HeroImage { ImageUrl = urls[i], SortOrder = i });
                }
            }
            db.SaveChanges();
        }

        // =========================================================
        // Industrial Content
        // =========================================================
        public static Dictionary<string, Dictionary<string, string>> GetIndustrialContent(AppDbContext db)
        {
            var result = new Dictionary<string, Dictionary<string, string>>
            {
                ["ar"] = new Dictionary<string, string>(),
                ["en"] = new Dictionary<string, string>(),
                ["ku"] = new Dictionary<string, string>()
            };
            foreach (var t in db.IndustrialContentTranslations.ToList())
            {
                if (!result.ContainsKey(t.Lang))
                    result[t.Lang] = new Dictionary<string, string>();
                result[t.Lang][t.Key] = t.Value;
            }
            return result;
        }

        public static string GetMapEmbedUrl(AppDbContext db)
        {
            var setting = db.IndustrialSettings.FirstOrDefault();
            return setting?.MapEmbedUrl ?? "";
        }

        public static void SaveIndustrialContent(AppDbContext db, Dictionary<string, Dictionary<string, string>> data, string mapEmbedUrl)
        {
            db.IndustrialContentTranslations.RemoveRange(db.IndustrialContentTranslations);
            db.SaveChanges();

            foreach (var lang in data.Keys)
            {
                foreach (var kv in data[lang])
                {
                    db.IndustrialContentTranslations.Add(new IndustrialContentTranslation
                    {
                        Lang = lang,
                        Key = kv.Key,
                        Value = kv.Value ?? ""
                    });
                }
            }

            var setting = db.IndustrialSettings.FirstOrDefault();
            if (setting == null)
            {
                db.IndustrialSettings.Add(new IndustrialSetting { MapEmbedUrl = mapEmbedUrl ?? "" });
            }
            else
            {
                setting.MapEmbedUrl = mapEmbedUrl ?? "";
            }

            db.SaveChanges();
        }

        // =========================================================
        // Guide Content
        // =========================================================
        public static List<GuideTopic> GetGuideTopics(AppDbContext db) =>
            db.GuideTopics.OrderBy(t => t.SortOrder).ToList();

        public static List<GuideEquipment> GetGuideEquipment(AppDbContext db) =>
            db.GuideEquipment.OrderBy(e => e.SortOrder).ToList();

        public static GuideTopic AddGuideTopic(AppDbContext db, GuideTopic topic)
        {
            topic.Id = 0;
            db.GuideTopics.Add(topic);
            db.SaveChanges();
            return topic;
        }

        public static bool UpdateGuideTopic(AppDbContext db, int id, GuideTopic updated)
        {
            var t = db.GuideTopics.Find(id);
            if (t == null) return false;
            t.Icon = updated.Icon;
            t.TitleAr = updated.TitleAr;
            t.TitleEn = updated.TitleEn;
            t.TitleKu = updated.TitleKu;
            t.BodyAr = updated.BodyAr;
            t.BodyEn = updated.BodyEn;
            t.BodyKu = updated.BodyKu;
            t.SortOrder = updated.SortOrder;
            db.SaveChanges();
            return true;
        }

        public static bool DeleteGuideTopic(AppDbContext db, int id)
        {
            var t = db.GuideTopics.Find(id);
            if (t == null) return false;
            db.GuideTopics.Remove(t);
            db.SaveChanges();
            return true;
        }

        public static GuideEquipment AddGuideEquip(AppDbContext db, GuideEquipment eq)
        {
            eq.Id = 0;
            db.GuideEquipment.Add(eq);
            db.SaveChanges();
            return eq;
        }

        public static bool UpdateGuideEquip(AppDbContext db, int id, GuideEquipment updated)
        {
            var e = db.GuideEquipment.Find(id);
            if (e == null) return false;
            e.NameAr = updated.NameAr;
            e.NameEn = updated.NameEn;
            e.NameKu = updated.NameKu;
            e.SpecAr = updated.SpecAr;
            e.SpecEn = updated.SpecEn;
            e.SpecKu = updated.SpecKu;
            e.UseAr = updated.UseAr;
            e.UseEn = updated.UseEn;
            e.UseKu = updated.UseKu;
            e.SortOrder = updated.SortOrder;
            db.SaveChanges();
            return true;
        }

        public static bool DeleteGuideEquip(AppDbContext db, int id)
        {
            var e = db.GuideEquipment.Find(id);
            if (e == null) return false;
            db.GuideEquipment.Remove(e);
            db.SaveChanges();
            return true;
        }

        // =========================================================
        // Helpers
        // =========================================================
        public static string Hash(string input)
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