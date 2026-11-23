// =============================================================================
// ME Workshop — Middle Euphrates Workshop · photo & video gallery
// Media files live in <data>/gallery (on Railway that folder is a volume, so
// uploads survive redeploys); the list itself is <data>/gallery.json.
// Public:  /gallery page, GET /api/gallery, files under /media/gallery/<name>
// Staff:   /api/admin/gallery (Admin and Manager) — upload, edit captions, move, delete
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace ConsanWebApp
{
    public class GalleryItem
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "image"; // "image" | "video"
        public string Url { get; set; } = "";
        public string File { get; set; } = "";
        public string CaptionAr { get; set; } = "";
        public string CaptionEn { get; set; } = "";
        public string CaptionKu { get; set; } = "";
        public string CaptionFa { get; set; } = "";
        public string CaptionTr { get; set; } = "";
        public long Size { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public static partial class Store
    {
        public const int MaxGalleryItems = 60;
        public const long MaxGalleryImageBytes = 12L * 1024 * 1024;   // 12 MB
        public const long MaxGalleryVideoBytes = 60L * 1024 * 1024;   // 60 MB

        private static readonly object GalleryLock = new object();
        private static List<GalleryItem>? _gallery;

        private static string GalleryPath { get { return Path.Combine(_dataDir, "gallery.json"); } }
        public static string GalleryDir { get { return Path.Combine(_dataDir, "gallery"); } }
        public static string DataDir { get { return _dataDir; } }

        private static List<GalleryItem> GalleryList()
        {
            // Loaded on first use so Store.Initialize stays untouched.
            if (_gallery == null)
            {
                Directory.CreateDirectory(GalleryDir);
                _gallery = LoadOrDefault(GalleryPath, () => new List<GalleryItem>());
            }
            return _gallery;
        }

        public static List<GalleryItem> GetGallery()
        {
            lock (GalleryLock) { return GalleryList().ToList(); }
        }

        public static int GalleryCount()
        {
            lock (GalleryLock) { return GalleryList().Count; }
        }

        /// <summary>Adds an item to the front of the gallery. Returns false when the gallery is full.</summary>
        public static bool TryAddGalleryItem(GalleryItem item)
        {
            lock (GalleryLock)
            {
                var list = GalleryList();
                if (list.Count >= MaxGalleryItems) return false;
                list.Insert(0, item);
                Persist(GalleryPath, list);
                return true;
            }
        }

        public static bool UpdateGalleryCaptions(string id, string ar, string en, string ku, string fa = "", string tr = "")
        {
            lock (GalleryLock)
            {
                var item = GalleryList().FirstOrDefault(g => g.Id == id);
                if (item == null) return false;
                item.CaptionAr = Trim(ar, 300);
                item.CaptionEn = Trim(en, 300);
                item.CaptionKu = Trim(ku, 300);
                item.CaptionFa = Trim(fa, 300);
                item.CaptionTr = Trim(tr, 300);
                Persist(GalleryPath, _gallery);
                return true;
            }
        }

        public static bool MoveGalleryItem(string id, int delta)
        {
            lock (GalleryLock)
            {
                var list = GalleryList();
                int i = list.FindIndex(g => g.Id == id);
                if (i < 0) return false;
                int j = Math.Clamp(i + delta, 0, list.Count - 1);
                if (i == j) return true;
                var item = list[i];
                list.RemoveAt(i);
                list.Insert(j, item);
                Persist(GalleryPath, list);
                return true;
            }
        }

        public static bool DeleteGalleryItem(string id)
        {
            GalleryItem? removed;
            lock (GalleryLock)
            {
                var list = GalleryList();
                removed = list.FirstOrDefault(g => g.Id == id);
                if (removed == null) return false;
                list.Remove(removed);
                Persist(GalleryPath, list);
            }
            TryDeleteGalleryFile(removed.File);
            return true;
        }

        public static void TryDeleteGalleryFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains("..")) return;
                string full = Path.Combine(GalleryDir, fileName);
                if (File.Exists(full)) File.Delete(full);
            }
            catch { }
        }

        private static string Trim(string? s, int max)
        {
            s = (s ?? "").Trim();
            return s.Length > max ? s.Substring(0, max) : s;
        }
    }

    public static class GalleryFeature
    {
        private static readonly Dictionary<string, string> ImageTypes = new Dictionary<string, string>
        {
            [".jpg"] = "jpeg", [".jpeg"] = "jpeg", [".png"] = "png", [".webp"] = "webp", [".gif"] = "gif"
        };
        private static readonly Dictionary<string, string> VideoTypes = new Dictionary<string, string>
        {
            [".mp4"] = "iso", [".m4v"] = "iso", [".mov"] = "iso", [".webm"] = "webm"
        };

        /// <summary>Serves the uploaded media. Call after the main UseStaticFiles.</summary>
        public static void UseGalleryFiles(WebApplication app)
        {
            Directory.CreateDirectory(Store.GalleryDir);
            var types = new FileExtensionContentTypeProvider();
            types.Mappings[".mov"] = "video/quicktime";
            types.Mappings[".m4v"] = "video/mp4";
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Store.GalleryDir),
                RequestPath = "/media/gallery",
                ContentTypeProvider = types,
                OnPrepareResponse = ctx =>
                {
                    // File names are random and never reused, so they can be cached for good.
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
                    ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                }
            });
        }

        public static void MapGallery(WebApplication app)
        {
            app.MapGet("/gallery", () => Results.Content(Pages.GetGalleryPage(), "text/html; charset=utf-8"));

            app.MapGet("/api/gallery", () => Results.Json(Store.GetGallery().Select(PublicView)));

            app.MapGet("/api/admin/gallery", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(new
                {
                    max = Store.MaxGalleryItems,
                    maxImageMb = Store.MaxGalleryImageBytes / (1024 * 1024),
                    maxVideoMb = Store.MaxGalleryVideoBytes / (1024 * 1024),
                    items = Store.GetGallery()
                });
            });

            app.MapPost("/api/admin/gallery", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                if (Store.GalleryCount() >= Store.MaxGalleryItems)
                    return Results.Json(new { message = "المعرض ممتلئ: الحد الأقصى " + Store.MaxGalleryItems + " ملفاً. احذف ملفاً أولاً." }, statusCode: 409);
                if (!ctx.Request.HasFormContentType) return Results.Json(new { message = "expected form data" }, statusCode: 400);

                IFormCollection form;
                try { form = await ctx.Request.ReadFormAsync(); }
                catch (Exception) { return Results.Json(new { message = "الملف كبير جداً أو الرفع لم يكتمل" }, statusCode: 413); }

                var file = form.Files["file"];
                if (file == null || file.Length == 0) return Results.Json(new { message = "لم يتم اختيار ملف" }, statusCode: 400);

                string ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
                bool isImage = ImageTypes.ContainsKey(ext);
                bool isVideo = VideoTypes.ContainsKey(ext);
                if (!isImage && !isVideo)
                    return Results.Json(new { message = "نوع الملف غير مدعوم. الصور: JPG, PNG, WEBP, GIF — الفيديو: MP4, MOV, WEBM" }, statusCode: 400);

                long max = isImage ? Store.MaxGalleryImageBytes : Store.MaxGalleryVideoBytes;
                if (file.Length > max)
                    return Results.Json(new { message = (isImage ? "الصورة" : "الفيديو") + " أكبر من الحد المسموح (" + (max / (1024 * 1024)) + " MB)" }, statusCode: 413);

                // The bytes must match the extension, so nothing else can be passed off as media.
                byte[] head = new byte[16];
                int read;
                using (var s = file.OpenReadStream()) { read = await s.ReadAsync(head, 0, head.Length); }
                string kind = isImage ? ImageTypes[ext] : VideoTypes[ext];
                if (!Matches(head, read, kind))
                    return Results.Json(new { message = "محتوى الملف لا يطابق امتداده" }, statusCode: 400);

                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(Store.GalleryDir)) ?? "/");
                    if (drive.AvailableFreeSpace < file.Length + 50L * 1024 * 1024)
                        return Results.Json(new { message = "لا توجد مساحة تخزين كافية على الخادم" }, statusCode: 507);
                }
                catch { /* free space unknown on this platform: carry on */ }

                Directory.CreateDirectory(Store.GalleryDir);
                string id = Guid.NewGuid().ToString("N");
                string name = id + (ext == ".jpeg" ? ".jpg" : ext);
                string full = Path.Combine(Store.GalleryDir, name);
                using (var stream = File.Create(full))
                {
                    await file.CopyToAsync(stream);
                }

                var item = new GalleryItem
                {
                    Id = id,
                    Type = isImage ? "image" : "video",
                    File = name,
                    Url = "/media/gallery/" + name,
                    CaptionAr = Clip(form["captionAr"]),
                    CaptionEn = Clip(form["captionEn"]),
                    CaptionKu = Clip(form["captionKu"]),
                    CaptionFa = Clip(form["captionFa"]),
                    CaptionTr = Clip(form["captionTr"]),
                    Size = file.Length,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                };

                if (!Store.TryAddGalleryItem(item))
                {
                    Store.TryDeleteGalleryFile(name);
                    return Results.Json(new { message = "المعرض ممتلئ: الحد الأقصى " + Store.MaxGalleryItems + " ملفاً. احذف ملفاً أولاً." }, statusCode: 409);
                }

                return Results.Json(new { success = true, item, count = Store.GalleryCount(), max = Store.MaxGalleryItems });
            })
            .WithMetadata(new RequestSizeLimitAttribute(Store.MaxGalleryVideoBytes + 2L * 1024 * 1024))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = Store.MaxGalleryVideoBytes + 2L * 1024 * 1024 });

            // Chunked upload: the dashboard sends files in pieces of up to 4 MB, so a large
            // video on a slow connection never runs into the hosting proxy's request timeout.
            // Pieces must arrive in order; the last one turns the upload into a gallery item.
            app.MapPost("/api/admin/gallery/chunk", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                if (!ctx.Request.HasFormContentType) return Results.Json(new { message = "expected form data" }, statusCode: 400);

                IFormCollection form;
                try { form = await ctx.Request.ReadFormAsync(); }
                catch (Exception) { return Results.Json(new { message = "تعذر استلام جزء الملف، أعد المحاولة" }, statusCode: 400); }

                string uploadId = form["uploadId"].ToString();
                if (!System.Text.RegularExpressions.Regex.IsMatch(uploadId, "^[a-f0-9]{32}$"))
                    return Results.Json(new { message = "invalid upload id" }, statusCode: 400);
                if (!int.TryParse(form["index"], out int index) || !int.TryParse(form["total"], out int total) || total < 1 || total > 200 || index < 0 || index >= total)
                    return Results.Json(new { message = "invalid chunk" }, statusCode: 400);
                var chunk = form.Files["chunk"];
                if (chunk == null || chunk.Length == 0 || chunk.Length > ChunkMax)
                    return Results.Json(new { message = "invalid chunk" }, statusCode: 400);

                string partDir = Path.Combine(Store.GalleryDir, ".partial");
                Directory.CreateDirectory(partDir);
                string partPath = Path.Combine(partDir, uploadId);
                PendingUpload? up;

                if (index == 0)
                {
                    CleanStalePartials(partDir);
                    if (Store.GalleryCount() >= Store.MaxGalleryItems)
                        return Results.Json(new { message = "المعرض ممتلئ: الحد الأقصى " + Store.MaxGalleryItems + " ملفاً. احذف ملفاً أولاً." }, statusCode: 409);

                    string ext = Path.GetExtension(form["fileName"].ToString()).ToLowerInvariant();
                    bool isImage = ImageTypes.ContainsKey(ext);
                    bool isVideo = VideoTypes.ContainsKey(ext);
                    if (!isImage && !isVideo)
                        return Results.Json(new { message = "نوع الملف غير مدعوم. الصور: JPG, PNG, WEBP, GIF — الفيديو: MP4, MOV, WEBM" }, statusCode: 400);
                    long.TryParse(form["size"], out long declared);
                    long max = isImage ? Store.MaxGalleryImageBytes : Store.MaxGalleryVideoBytes;
                    if (declared <= 0 || declared > max)
                        return Results.Json(new { message = (isImage ? "الصورة" : "الفيديو") + " أكبر من الحد المسموح (" + (max / (1024 * 1024)) + " MB)" }, statusCode: 413);

                    byte[] head = new byte[16];
                    int read;
                    using (var s = chunk.OpenReadStream()) { read = await s.ReadAsync(head, 0, head.Length); }
                    if (!Matches(head, read, isImage ? ImageTypes[ext] : VideoTypes[ext]))
                        return Results.Json(new { message = "محتوى الملف لا يطابق امتداده" }, statusCode: 400);

                    try
                    {
                        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(Store.GalleryDir)) ?? "/");
                        if (drive.AvailableFreeSpace < declared + 50L * 1024 * 1024)
                            return Results.Json(new { message = "لا توجد مساحة تخزين كافية على الخادم" }, statusCode: 507);
                    }
                    catch { }

                    up = new PendingUpload { Ext = ext == ".jpeg" ? ".jpg" : ext, IsImage = isImage, Max = max, Total = total };
                    Pending[uploadId] = up;
                    if (File.Exists(partPath)) File.Delete(partPath);
                }
                else if (!Pending.TryGetValue(uploadId, out up) || up.Total != total)
                {
                    return Results.Json(new { message = "انتهت جلسة الرفع، أعد رفع الملف" }, statusCode: 410);
                }

                lock (up)
                {
                    if (index != up.Next)
                    {
                        // A retried piece that was already stored: acknowledge it without writing twice.
                        if (index == up.Next - 1) return Results.Json(new { success = true, received = index });
                        return Results.Json(new { message = "ترتيب الأجزاء غير صحيح، أعد رفع الملف" }, statusCode: 409);
                    }
                    if (up.Bytes + chunk.Length > up.Max)
                    {
                        Pending.TryRemove(uploadId, out _);
                        try { File.Delete(partPath); } catch { }
                        return Results.Json(new { message = "الملف أكبر من الحد المسموح" }, statusCode: 413);
                    }
                    using (var fs = new FileStream(partPath, FileMode.Append, FileAccess.Write))
                    using (var cs = chunk.OpenReadStream())
                    {
                        cs.CopyTo(fs);
                    }
                    up.Bytes += chunk.Length;
                    up.Next++;
                    up.Touched = DateTime.UtcNow;
                }

                if (index < total - 1) return Results.Json(new { success = true, received = index });

                // Last piece: publish it.
                Pending.TryRemove(uploadId, out _);
                string id = Guid.NewGuid().ToString("N");
                string name = id + up.Ext;
                File.Move(partPath, Path.Combine(Store.GalleryDir, name));
                var item = new GalleryItem
                {
                    Id = id,
                    Type = up.IsImage ? "image" : "video",
                    File = name,
                    Url = "/media/gallery/" + name,
                    CaptionAr = Clip(form["captionAr"]),
                    CaptionEn = Clip(form["captionEn"]),
                    CaptionKu = Clip(form["captionKu"]),
                    CaptionFa = Clip(form["captionFa"]),
                    CaptionTr = Clip(form["captionTr"]),
                    Size = up.Bytes,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                };
                if (!Store.TryAddGalleryItem(item))
                {
                    Store.TryDeleteGalleryFile(name);
                    return Results.Json(new { message = "المعرض ممتلئ: الحد الأقصى " + Store.MaxGalleryItems + " ملفاً. احذف ملفاً أولاً." }, statusCode: 409);
                }
                return Results.Json(new { success = true, item, count = Store.GalleryCount(), max = Store.MaxGalleryItems });
            })
            .WithMetadata(new RequestSizeLimitAttribute(ChunkMax + 1024 * 1024))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = ChunkMax + 1024 * 1024 });

            app.MapPut("/api/admin/gallery/{id}", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("captionAr", out var ar);
                body.TryGetValue("captionEn", out var en);
                body.TryGetValue("captionKu", out var ku);
                body.TryGetValue("captionFa", out var fa);
                body.TryGetValue("captionTr", out var tr);
                return Store.UpdateGalleryCaptions(id, ar ?? "", en ?? "", ku ?? "", fa ?? "", tr ?? "")
                    ? Results.Json(new { success = true })
                    : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapPost("/api/admin/gallery/{id}/move", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("dir", out var dir);
                int delta = dir == "up" ? -1 : 1;
                return Store.MoveGalleryItem(id, delta)
                    ? Results.Json(new { success = true })
                    : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapDelete("/api/admin/gallery/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Store.DeleteGalleryItem(id)
                    ? Results.Json(new { success = true, count = Store.GalleryCount() })
                    : Results.Json(new { message = "not found" }, statusCode: 404);
            });
        }

        private const long ChunkMax = 5L * 1024 * 1024; // the dashboard sends 4 MB pieces

        private class PendingUpload
        {
            public string Ext = "";
            public bool IsImage;
            public long Max;
            public int Total;
            public int Next;
            public long Bytes;
            public DateTime Touched = DateTime.UtcNow;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PendingUpload> Pending =
            new System.Collections.Concurrent.ConcurrentDictionary<string, PendingUpload>();

        // Drops uploads nobody has touched for two hours (a closed tab, a lost connection).
        private static void CleanStalePartials(string partDir)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-2);
                foreach (var kv in Pending)
                    if (kv.Value.Touched < cutoff) Pending.TryRemove(kv.Key, out _);
                foreach (var f in Directory.GetFiles(partDir))
                    if (File.GetLastWriteTimeUtc(f) < cutoff) File.Delete(f);
            }
            catch { }
        }

        public static object PublicView(GalleryItem g)
        {
            return new { id = g.Id, type = g.Type, url = g.Url, captionAr = g.CaptionAr, captionEn = g.CaptionEn, captionKu = g.CaptionKu, captionFa = g.CaptionFa, captionTr = g.CaptionTr };
        }

        private static string Clip(string? s)
        {
            s = (s ?? "").Trim();
            return s.Length > 300 ? s.Substring(0, 300) : s;
        }

        internal static bool Matches(byte[] b, int n, string kind)
        {
            switch (kind)
            {
                case "jpeg": return n >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;
                case "png": return n >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
                case "gif": return n >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38;
                case "webp": return n >= 12 && Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && Encoding.ASCII.GetString(b, 8, 4) == "WEBP";
                case "iso": return n >= 8 && Encoding.ASCII.GetString(b, 4, 4) == "ftyp";
                case "webm": return n >= 4 && b[0] == 0x1A && b[1] == 0x45 && b[2] == 0xDF && b[3] == 0xA3;
            }
            return false;
        }
    }

    public static partial class Pages
    {
        // One tile. The same markup is used on the gallery page and the home-page strip.
        public static string GalleryTile(GalleryItem g, int index)
        {
            string url = System.Net.WebUtility.HtmlEncode(g.Url);
            string cap = System.Net.WebUtility.HtmlEncode(g.CaptionAr ?? "");
            var sb = new StringBuilder();
            sb.Append("<li class=\"g-tile\" data-type=\"").Append(g.Type).Append("\">");
            sb.Append("<button type=\"button\" class=\"g-open\" data-index=\"").Append(index).Append("\" aria-label=\"").Append(cap.Length > 0 ? cap : (g.Type == "video" ? "Video" : "Photo")).Append("\">");
            if (g.Type == "video")
            {
                sb.Append("<video src=\"").Append(url).Append("#t=0.1\" preload=\"metadata\" muted playsinline tabindex=\"-1\" aria-hidden=\"true\"></video>");
                sb.Append("<span class=\"g-play\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\"><path d=\"M8 5.5v13l11-6.5z\" fill=\"currentColor\"/></svg></span>");
            }
            else
            {
                sb.Append("<img src=\"").Append(url).Append("\" alt=\"").Append(cap).Append("\" loading=\"lazy\" decoding=\"async\" />");
            }
            sb.Append("<span class=\"g-cap\" data-cap-index=\"").Append(index).Append("\">").Append(cap).Append("</span>");
            sb.Append("</button></li>");
            return sb.ToString();
        }

        public static string GetGalleryPage()
        {
            var items = Store.GetGallery();
            int photos = items.Count(i => i.Type == "image");
            int videos = items.Count(i => i.Type == "video");

            var tiles = new StringBuilder();
            for (int i = 0; i < items.Count; i++) tiles.Append(GalleryTile(items[i], i));

            string body = """
            <body class="page-gallery">
                %%NAV%%
                <main class="sec gallery-page">
                    <div class="container">
                        <header class="gallery-head">
                            <div>
                                <h1 data-i18n="galleryTitle">[[galleryTitle]]</h1>
                                <p class="lead" data-i18n="gallerySub">[[gallerySub]]</p>
                            </div>
                            <div class="g-filters" role="group" aria-label="Filter">
                                <button type="button" data-filter="all" aria-pressed="true"><span data-i18n="galleryAll">[[galleryAll]]</span> <b>%%ALL%%</b></button>
                                <button type="button" data-filter="image" aria-pressed="false"><span data-i18n="galleryPhotos">[[galleryPhotos]]</span> <b>%%PHOTOS%%</b></button>
                                <button type="button" data-filter="video" aria-pressed="false"><span data-i18n="galleryVideos">[[galleryVideos]]</span> <b>%%VIDEOS%%</b></button>
                            </div>
                        </header>
                        %%GRID%%
                    </div>
                </main>
                %%FOOTER%%
                %%CONTACT%%
                %%LIGHTBOX%%
                <script defer src="/me/gallery.js?v=[[V]]"></script>
            </body>
            """;

            string grid = items.Count == 0
                ? "<p class=\"g-empty\" data-i18n=\"galleryEmpty\">[[galleryEmpty]]</p>"
                : "<ul class=\"g-grid\" data-gallery=\"page\">" + tiles + "</ul>";

            body = body.Replace("%%NAV%%", GetNavbarHtml())
                       .Replace("%%ALL%%", items.Count.ToString())
                       .Replace("%%PHOTOS%%", photos.ToString())
                       .Replace("%%VIDEOS%%", videos.ToString())
                       .Replace("%%GRID%%", grid)
                       .Replace("%%FOOTER%%", GetFooterHtml())
                       .Replace("%%CONTACT%%", GetContactWidgetHtml())
                       .Replace("%%LIGHTBOX%%", GalleryLightboxHtml());

            var extra = new Dictionary<string, object>
            {
                ["gallery"] = items.Select(GalleryFeature.PublicView).ToList()
            };
            return PageShell("المعرض | ME Workshop — Middle Euphrates Workshop", "", body, extra);
        }

        // Home-page strip: the first eight items, or nothing when the gallery is empty.
        public static string GalleryHomeSection(List<GalleryItem> items)
        {
            if (items.Count == 0) return "";
            var tiles = new StringBuilder();
            int n = Math.Min(8, items.Count);
            for (int i = 0; i < n; i++) tiles.Append(GalleryTile(items[i], i));
            return """
                <section class="sec sec-steel" id="gallery">
                    <div class="container">
                        <div class="gallery-head">
                            <div>
                                <h2 data-i18n="galleryHomeTitle">[[galleryHomeTitle]]</h2>
                                <p class="lead" data-i18n="gallerySub">[[gallerySub]]</p>
                            </div>
                            <a class="btn btn-line" href="/gallery" data-i18n="galleryViewAll">[[galleryViewAll]]</a>
                        </div>
                        <ul class="g-grid g-grid-home" data-gallery="home">%%TILES%%</ul>
                    </div>
                </section>
            """.Replace("%%TILES%%", tiles.ToString()) + GalleryLightboxHtml() + "<script defer src=\"/me/gallery.js?v=[[V]]\"></script>";
        }

        public static string GalleryLightboxHtml()
        {
            return """
            <div class="lightbox" id="lightbox" role="dialog" aria-modal="true" aria-label="Gallery" hidden>
                <div class="lb-top">
                    <span class="lb-count" id="lbCount"></span>
                    <button type="button" class="lb-btn lb-close" id="lbClose" data-i18n-label="galleryClose" aria-label="[[galleryClose]]"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6L6 18"/></svg></button>
                </div>
                <div class="lb-stage" id="lbStage"></div>
                <p class="lb-cap" id="lbCap"></p>
                <div class="lb-bar">
                    <button type="button" class="lb-btn lb-nav lb-prev" id="lbPrev" data-i18n-label="galleryPrev" aria-label="[[galleryPrev]]"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M15 5l-7 7 7 7"/></svg></button>
                    <button type="button" class="lb-btn lb-nav lb-next" id="lbNext" data-i18n-label="galleryNext" aria-label="[[galleryNext]]"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 5l7 7-7 7"/></svg></button>
                </div>
            </div>
            """;
        }
    }
}
