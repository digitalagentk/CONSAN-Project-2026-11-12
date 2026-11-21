// =============================================================================
// ME Workshop — site images (hero slider, timeline / archival album, explorer cards)
// Staff (Admin and Manager) pick a photo from their computer; it is uploaded to
// <data>/uploads (a volume on Railway and in docker-compose, so it survives
// redeploys) and served at /uploads/<name>. Files uploaded before this change
// sit in wwwroot/uploads and are still served from there.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace ConsanWebApp
{
    public static partial class Store
    {
        public const long MaxSiteImageBytes = 12L * 1024 * 1024;   // 12 MB
        public const int MaxHeroImages = 8;
        public static string UploadsDir { get { return Path.Combine(_dataDir, "uploads"); } }

        /// <summary>Replaces only the hero slider photos, leaving the page text alone.</summary>
        public static void SaveHeroImages(List<string> images)
        {
            lock (ContentLock)
            {
                _content.HeroImages = images;
                Persist(ContentPath, _content);
            }
        }
    }

    public static class SiteImages
    {
        private static readonly Dictionary<string, string> Types = new Dictionary<string, string>
        {
            [".jpg"] = "jpeg", [".jpeg"] = "jpeg", [".png"] = "png", [".webp"] = "webp", [".gif"] = "gif"
        };

        /// <summary>Serves /uploads from the data volume. Call after the main UseStaticFiles.</summary>
        public static void UseUploadFiles(WebApplication app)
        {
            Directory.CreateDirectory(Store.UploadsDir);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.GetFullPath(Store.UploadsDir)),
                RequestPath = "/uploads",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
                    ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                }
            });
        }

        public static void MapSiteImages(WebApplication app)
        {
            // Upload one photo from the staff member's computer. Returns its URL.
            app.MapPost("/api/admin/upload-image", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                if (!ctx.Request.HasFormContentType) return Results.Json(new { message = "expected form data" }, statusCode: 400);

                IFormCollection form;
                try { form = await ctx.Request.ReadFormAsync(); }
                catch (Exception) { return Results.Json(new { message = "الصورة كبيرة جداً أو الرفع لم يكتمل" }, statusCode: 413); }

                var file = form.Files["file"];
                if (file == null || file.Length == 0) return Results.Json(new { message = "لم يتم اختيار صورة" }, statusCode: 400);

                string ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
                if (!Types.ContainsKey(ext))
                    return Results.Json(new { message = "نوع الملف غير مدعوم. الصور المسموحة: JPG, PNG, WEBP, GIF" }, statusCode: 400);
                if (file.Length > Store.MaxSiteImageBytes)
                    return Results.Json(new { message = "الصورة أكبر من " + (Store.MaxSiteImageBytes / (1024 * 1024)) + " MB" }, statusCode: 413);

                // The bytes must match the extension, so nothing else can be passed off as an image.
                byte[] head = new byte[16];
                int read;
                using (var s = file.OpenReadStream()) { read = await s.ReadAsync(head, 0, head.Length); }
                if (!GalleryFeature.Matches(head, read, Types[ext]))
                    return Results.Json(new { message = "محتوى الملف لا يطابق امتداده" }, statusCode: 400);

                Directory.CreateDirectory(Store.UploadsDir);
                string name = Guid.NewGuid().ToString("N") + (ext == ".jpeg" ? ".jpg" : ext);
                using (var stream = File.Create(Path.Combine(Store.UploadsDir, name)))
                {
                    await file.CopyToAsync(stream);
                }
                return Results.Json(new { success = true, url = "/uploads/" + name });
            })
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = Store.MaxSiteImageBytes + 1024 * 1024 });

            // Hero slider photos only (Admin and Manager); the page text is saved separately.
            app.MapPut("/api/admin/hero-images", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                List<string>? list;
                try { list = await System.Text.Json.JsonSerializer.DeserializeAsync<List<string>>(ctx.Request.Body, Store.ApiJsonOptions); }
                catch { list = null; }
                if (list == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var clean = list.Select(u => (u ?? "").Trim()).Where(u => u.Length > 0).Take(Store.MaxHeroImages).ToList();
                foreach (var u in clean)
                {
                    bool local = u.StartsWith("/") && !u.StartsWith("//");
                    bool web = u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || u.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
                    if (u.Length > 500 || (!local && !web)) return Results.Json(new { message = "رابط صورة غير صالح: " + u }, statusCode: 400);
                }
                Store.SaveHeroImages(clean);
                return Results.Json(new { success = true, heroImages = clean });
            });
        }
    }
}
