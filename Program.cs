// =============================================================================
// ME Workshop — Middle Euphrates Workshop · web app + admin dashboard
// Public pages: Site/Shared.cs, Site/HomePage.cs, wwwroot/me/*. Inner pages and admin: below.
// -----------------------------------------------------------------------------
// PROJECT SETUP (Rider):
//   The .csproj MUST use the ASP.NET Core "Web" SDK, e.g.:
//     <Project Sdk="Microsoft.NET.Sdk.Web">
//       <PropertyGroup>
//         <TargetFramework>net8.0</TargetFramework>
//         <ImplicitUsings>disable</ImplicitUsings>
//         <Nullable>disable</Nullable>
//       </PropertyGroup>
//
// ADMIN LOGIN:
//   URL:      http://localhost:8080/admin
//   Password: admin   (change AdminPassword in Store below)
//
// DATA:
//   Everything (accounts, service requests, editable page content, map pins)
//   is persisted as JSON files under ./data, created automatically on first run.
//   Uploaded images are saved under ./wwwroot/uploads and served as static files.
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using ConsanWebApp.Data;
using ConsanWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ConsanWebApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== Connection String =====
            var connectionString = System.Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? ("Server=127.0.0.1,1433;Database=ConsanDb;User Id=sa;Password="
                    + (System.Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD") ?? "")
                    + ";TrustServerCertificate=True;MultipleActiveResultSets=true");

                        // ===== DbContext =====
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // ===== Authentication (Cookie) + Authorization =====
            builder.Services.AddAuthentication("ConsanAuth")
                .AddCookie("ConsanAuth", options =>
                {
                    options.Cookie.Name = "consan_auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.SlidingExpiration = true;
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                options.AddPolicy("DashboardAccess", p => p.RequireRole("Admin", "Manager"));
            });

            // ===== Data Protection (persist keys to disk — survive rebuilds) =====
            var keysPath = System.Environment.GetEnvironmentVariable("ASPNETCORE_KEYS_PATH") 
                ?? "/app/data/protection-keys";
            System.IO.Directory.CreateDirectory(keysPath);
            
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysPath))
                .SetApplicationName("ConsanApp");

            // ===== Server URLs (0.0.0.0 for Docker, localhost for local dev) =====
            var serverUrls = System.Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                ?? "http://0.0.0.0:8080/";
            builder.WebHost.UseUrls(serverUrls);

            var app = builder.Build();

            // ===== Auto-migrate (with dedicated admin connection for DDL) =====
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    // استخدام connection منفصل بصلاحيات sa للـ migrations فقط
                    var migrationConnStr = System.Environment.GetEnvironmentVariable("ConnectionStrings__MigrationConnection");
                    
                    if (!string.IsNullOrWhiteSpace(migrationConnStr))
                    {
                        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>();
                        optionsBuilder.UseSqlServer(migrationConnStr);
                        using (var migrationDb = new AppDbContext(optionsBuilder.Options))
                        {
                            migrationDb.Database.Migrate();
                            Console.WriteLine("✅ Migrations applied with admin connection");
                        }
                    }
                    else
                    {
                        // fallback: استخدام نفس connection (للتطوير المحلي)
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        db.Database.Migrate();
                        Console.WriteLine("⚠️ Migrations applied with app connection (dev mode)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Migration failed: {ex.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                    // لا نتوقف — نحاول نكمل عشان الـ endpoints تشتغل حتى لو الـ DB فيها مشكلة
                }
            }

            string contentRoot = app.Environment.ContentRootPath;
            Directory.CreateDirectory(Path.Combine(contentRoot, "wwwroot"));
            Directory.CreateDirectory(Path.Combine(contentRoot, "wwwroot", "uploads"));

            // ===== Seed Data على DB =====
            try
            {
                using (var seedScope = app.Services.CreateScope())
                {
                    var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    SeedData.Initialize(db);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Seed failed: {ex.Message}");
            }

            // ===== Store (fallback مؤقت — سيُحذف تدريجياً) =====
            Store.Initialize(contentRoot);

            app.UseDefaultFiles();
            app.UseStaticFiles();
            GalleryFeature.UseGalleryFiles(app);

            // ⚠️ مهم: UseAuthentication و UseAuthorization قبل الـ endpoints
            app.UseAuthentication();
            app.UseAuthorization();

            // ===== Helper: قراءة JSON من الطلب =====
            async Task<T?> ReadJsonBody<T>(HttpContext ctx) where T : class
            {
                try
                {
                    return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
                        ctx.Request.Body,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { return null; }
            }

            // =========================================================
            // ======================  PUBLIC PAGES  ======================
            // =========================================================
            app.MapGet("/", () => Results.Content(Pages.GetHomePage(), "text/html; charset=utf-8"));
            app.MapGet("/about", () => Results.Redirect("/#story"));
            app.MapGet("/phone-numbers", () => Results.Redirect("/#contact"));
            app.MapGet("/login", () => Results.Content(Pages.GetLoginPage(), "text/html; charset=utf-8"));
            app.MapGet("/service", () => Results.Content(Pages.GetServiceRequestPage(), "text/html; charset=utf-8"));
            app.MapGet("/industrial-services", () => Results.Content(Pages.GetIndustrialServicesPage(), "text/html; charset=utf-8"));
            app.MapGet("/explorer", () => Results.Content(Pages.GetExplorerPage(), "text/html; charset=utf-8"));
            app.MapGet("/guide", () => Results.Content(Pages.GetGuidePage(), "text/html; charset=utf-8"));
            app.MapGet("/admin", () => Results.Content(Pages.GetAdminPage(), "text/html; charset=utf-8"));
            GalleryFeature.MapGallery(app);
            BillingFeature.MapBilling(app);
            VisitorFeature.MapVisitors(app);

            // Anything else falls back to the home page
            app.MapFallback(() => Results.Content(Pages.GetHomePage(), "text/html; charset=utf-8"));

            // =========================================================
            // ==================  PUBLIC CONTENT APIs  ===================
            // =========================================================
            app.MapGet("/api/content", () => Results.Json(Store.GetContent()));
            app.MapGet("/api/industrial-content", () => Results.Json(Store.GetIndustrialContent()));

            // =========================================================
            // ====================  MAP PINS (public)  ===================
            // =========================================================
            app.MapGet("/api/map-pins", () => Results.Json(Store.GetApprovedMapPins()));

            // =========================================================
            // ====================  EXPLORER CARDS (public)  =============
            // =========================================================
            app.MapGet("/api/explorer-cards", () => Results.Json(Store.GetExplorerCards()));

            // =========================================================
            // ====================  GUIDE CONTENT (public)  ==============
            // =========================================================
            app.MapGet("/api/guide-content", () => Results.Json(Store.GetGuideContent()));

            // =========================================================
            // ====================  ACCOUNT AUTH (Admin/Manager)  ========
            // =========================================================
            app.MapPost("/api/auth/login", async (HttpContext ctx, AppDbContext db) =>
            {
                var dto = await ReadJsonBody<LoginDto>(ctx);
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                    return Results.Json(new { success = false, message = "البريد وكلمة المرور مطلوبان" }, statusCode: 400);

                if (!DbService.IsValidEmail(dto.Email))
                    return Results.Json(new { success = false, message = "صيغة البريد غير صحيحة" }, statusCode: 400);

                var user = DbService.LoginByEmail(db, dto.Email, dto.Password);
                if (user == null)
                    return Results.Json(new { success = false, message = "بيانات الدخول غير صحيحة" }, statusCode: 401);

                var claims = new List<System.Security.Claims.Claim>
                {
                    new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(System.Security.Claims.ClaimTypes.Name, user.FullName),
                    new(System.Security.Claims.ClaimTypes.Email, user.Email),
                    new(System.Security.Claims.ClaimTypes.Role, user.Role)
                };
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "ConsanAuth");
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                await ctx.SignInAsync("ConsanAuth", principal,
                    new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                    });

                return Results.Json(new
                {
                    success = true,
                    user = new { user.Id, user.FullName, user.Email, user.Role }
                });
            });

            app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
            {
                await ctx.SignOutAsync("ConsanAuth");
                if (ctx.Request.Cookies.TryGetValue("consan_admin", out var token))
                {
                    Store.RevokeAdminToken(token);
                    ctx.Response.Cookies.Delete("consan_admin");
                }
                return Results.Json(new { success = true });
            });

            app.MapGet("/api/auth/me", (HttpContext ctx) =>
            {
                if (ctx.User?.Identity?.IsAuthenticated != true)
                    return Results.Json(new { authenticated = false }, statusCode: 401);

                return Results.Json(new
                {
                    authenticated = true,
                    user = new
                    {
                        id = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        fullName = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                        email = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                        role = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    }
                });
            });

            // =========================================================
            // ==================  SERVICE REQUESTS (public)  ================
            // =========================================================
            app.MapPost("/api/service-requests", async (HttpContext ctx) =>
            {
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("fullName", out var fullName);
                body.TryGetValue("phone", out var phone);
                body.TryGetValue("serviceType", out var serviceType);
                body.TryGetValue("message", out var message);
                var item = Store.AddRequest(fullName ?? "", phone ?? "", serviceType ?? "", message ?? "");
                return Results.Json(new { success = true, id = item.Id });
            });

            // =========================================================
            // ====================  MAP PINS (public submit)  =============
            // =========================================================
            // Visitors can only suggest a location: it is stored as pending and stays off the
            // public map until a Manager or Admin approves it in the dashboard.
            app.MapPost("/api/map-pins/submit", async (HttpContext ctx) =>
            {
                var pin = await Store.ReadMapPin(ctx);
                if (pin == null) return Results.Json(new { success = false, message = "invalid body" }, statusCode: 400);
                string err = Store.ValidatePin(pin);
                if (err != null) return Results.Json(new { success = false, message = err }, statusCode: 400);
                if (!Store.AllowPinSubmission(ctx)) return Results.Json(new { success = false, message = "تم إرسال عدد كبير من الطلبات، حاول لاحقاً" }, statusCode: 429);
                pin.IsApproved = false;
                pin.IsWorkshop = false;
                var item = Store.AddMapPin(pin);
                return Results.Json(new { success = true, pending = true, id = item.Id });
            });

            // =========================================================
            // ======================  ADMIN AUTH (check)  ===============
            // =========================================================
            app.MapGet("/api/admin/check", (HttpContext ctx) =>
            {
                bool isAdmin = ctx.User?.IsInRole("Admin") == true;
                bool isManager = ctx.User?.IsInRole("Manager") == true;
                bool isLegacyAdmin = Store.IsAdminAuthenticated(ctx);

                return Results.Json(new
                {
                    authenticated = isAdmin || isManager || isLegacyAdmin,
                    role = isAdmin ? "Admin" : (isManager ? "Manager" : (isLegacyAdmin ? "Admin" : null))
                });
            });

            // =========================================================
            // ============  ADMIN/MANAGER: ACCOUNTS MANAGEMENT  ==========
            // =========================================================

            // عرض جميع الحسابات — Admin و Manager
            app.MapGet("/api/admin/accounts", (HttpContext ctx, AppDbContext db) =>
            {
                if (ctx.User?.Identity?.IsAuthenticated != true)
                    return Results.Json(new { message = "unauthorized" }, statusCode: 401);

                return Results.Json(DbService.GetAccountsForList(db));
            });

            // إنشاء حساب جديد — Admin فقط
            app.MapPost("/api/admin/accounts", async (HttpContext ctx, AppDbContext db) =>
            {
                if (ctx.User?.IsInRole("Admin") != true)
                    return Results.Json(new { message = "غير مصرح — Admin فقط" }, statusCode: 403);

                var dto = await ReadJsonBody<CreateAccountDto>(ctx);
                if (dto == null)
                    return Results.Json(new { message = "بيانات غير صالحة" }, statusCode: 400);

                var (ok, error, created) = DbService.CreateAccount(db, dto);
                if (!ok) return Results.Json(new { message = error }, statusCode: 400);

                return Results.Json(new { success = true, id = created.Id });
            });

            // تغيير كلمة مرور حساب — Admin فقط
            app.MapPut("/api/admin/accounts/{id}/password", async (HttpContext ctx, AppDbContext db, int id) =>
            {
                if (ctx.User?.IsInRole("Admin") != true)
                    return Results.Json(new { message = "غير مصرح — Admin فقط" }, statusCode: 403);

                var dto = await ReadJsonBody<ChangePasswordDto>(ctx);
                if (dto == null)
                    return Results.Json(new { message = "بيانات غير صالحة" }, statusCode: 400);

                var (ok, error) = DbService.ChangeUserPassword(db, id, dto.NewPassword);
                if (!ok) return Results.Json(new { message = error }, statusCode: 400);

                return Results.Json(new { success = true });
            });

            // حذف حساب — Admin فقط (ولا يمكنه حذف نفسه)
            app.MapDelete("/api/admin/accounts/{id}", (HttpContext ctx, AppDbContext db, int id) =>
            {
                if (ctx.User?.IsInRole("Admin") != true)
                    return Results.Json(new { message = "غير مصرح — Admin فقط" }, statusCode: 403);

                var idClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idClaim, out int currentUserId))
                    return Results.Json(new { message = "جلسة غير صالحة" }, statusCode: 401);

                var (ok, error) = DbService.DeleteAccount(db, id, currentUserId);
                if (!ok) return Results.Json(new { message = error }, statusCode: 400);

                return Results.Json(new { success = true });
            });

            // =========================================================
            // ==================  ADMIN: USER ACCOUNTS  ===================
            // =========================================================
            app.MapGet("/api/admin/users", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetUsers());
            });

            app.MapDelete("/api/admin/users/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var ok = Store.DeleteUser(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            // =========================================================
            // ==================  ADMIN: SERVICE REQUESTS  ================
            // =========================================================
            app.MapGet("/api/admin/service-requests", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetRequests());
            });

            app.MapPut("/api/admin/service-requests/{id}", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("status", out var status);
                if (string.IsNullOrWhiteSpace(status)) return Results.Json(new { message = "status required" }, statusCode: 400);
                var ok = Store.UpdateRequestStatus(id, status);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapDelete("/api/admin/service-requests/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var ok = Store.DeleteRequest(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            // =========================================================
            // ============  ADMIN: HOME PAGE CONTENT MANAGER  =============
            // =========================================================
            app.MapPut("/api/admin/content", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var data = await Store.ReadSiteContent(ctx);
                if (data == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                Store.SaveContent(data);
                return Results.Json(new { success = true });
            });

            // =========================================================
            // ========  ADMIN: INDUSTRIAL SERVICES + MAP MANAGER  =========
            // =========================================================
            app.MapPut("/api/admin/industrial-content", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var data = await Store.ReadIndustrialContent(ctx);
                if (data == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                Store.SaveIndustrialContent(data);
                return Results.Json(new { success = true });
            });

            // =========================================================
            // ===============  ADMIN: MAP PINS (departments)  =============
            // =========================================================
            app.MapGet("/api/admin/map-pins", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetAllMapPins());
            });

            app.MapGet("/api/admin/map-pins/pending", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetPendingMapPins());
            });

            app.MapGet("/api/admin/map-pins/approved", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetApprovedMapPins());
            });

            app.MapPost("/api/admin/map-pins", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var pin = await Store.ReadMapPin(ctx);
                if (pin == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                string err = Store.ValidatePin(pin);
                if (err != null) return Results.Json(new { message = err }, statusCode: 400);
                pin.IsApproved = true;   // added by staff: already approved
                pin.IsWorkshop = false;
                var item = Store.AddMapPin(pin);
                return Results.Json(new { success = true, id = item.Id });
            });

            app.MapPut("/api/admin/map-pins/{id}", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var pin = await Store.ReadMapPin(ctx);
                if (pin == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var ok = Store.UpdateMapPin(id, pin);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapPut("/api/admin/map-pins/{id}/approve", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var ok = Store.ApproveMapPin(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapDelete("/api/admin/map-pins/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                if (Store.IsWorkshopPin(id)) return Results.Json(new { message = "موقع الورشة الرئيسي لا يُحذف، يمكن تعديله فقط" }, statusCode: 400);
                var ok = Store.DeleteMapPin(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            // =========================================================
            // ============  ADMIN: ARCHIVAL ALBUM MANAGER  ================
            // =========================================================
            app.MapGet("/api/admin/archival-album", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetArchivalAlbum());
            });

            app.MapPost("/api/admin/archival-album", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var item = await Store.ReadArchivalItem(ctx);
                if (item == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var created = Store.AddArchivalItem(item);
                return Results.Json(new { success = true, id = created.Id });
            });

            app.MapPut("/api/admin/archival-album/{id}", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var item = await Store.ReadArchivalItem(ctx);
                if (item == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var ok = Store.UpdateArchivalItem(id, item);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapDelete("/api/admin/archival-album/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var ok = Store.DeleteArchivalItem(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            // =========================================================
            // ====================  PUBLIC ARCHIVAL ALBUM  ================
            // =========================================================
            app.MapGet("/api/archival-album", () => Results.Json(Store.GetArchivalAlbum()));

            // =========================================================
            // ============  ADMIN: EXPLORER CARDS MANAGER  ================
            // =========================================================
            app.MapGet("/api/admin/explorer-cards", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetExplorerCards());
            });

            app.MapPost("/api/admin/explorer-cards", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var card = await Store.ReadExplorerCard(ctx);
                if (card == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var created = Store.AddExplorerCard(card);
                return Results.Json(new { success = true, id = created.Id });
            });

            app.MapPut("/api/admin/explorer-cards/{id}", async (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var card = await Store.ReadExplorerCard(ctx);
                if (card == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                var ok = Store.UpdateExplorerCard(id, card);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            app.MapDelete("/api/admin/explorer-cards/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var ok = Store.DeleteExplorerCard(id);
                return ok ? Results.Json(new { success = true }) : Results.Json(new { message = "not found" }, statusCode: 404);
            });

            // =========================================================
            // ============  ADMIN: GUIDE CONTENT MANAGER  =================
            // =========================================================
            app.MapGet("/api/admin/guide-content", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(Store.GetGuideContent());
            });

            app.MapPut("/api/admin/guide-content", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var data = await Store.ReadGuideContent(ctx);
                if (data == null) return Results.Json(new { message = "invalid body" }, statusCode: 400);
                Store.SaveGuideContent(data);
                return Results.Json(new { success = true });
            });

            // =========================================================
            // ==================  ADMIN: IMAGE UPLOAD  ====================
            // =========================================================
            app.MapPost("/api/admin/upload-image", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                if (!ctx.Request.HasFormContentType) return Results.Json(new { message = "expected form data" }, statusCode: 400);

                var form = await ctx.Request.ReadFormAsync();
                var file = form.Files["file"];
                if (file == null || file.Length == 0) return Results.Json(new { message = "no file uploaded" }, statusCode: 400);

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                string[] allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (Array.IndexOf(allowed, ext) < 0)
                    return Results.Json(new { message = "unsupported file type" }, statusCode: 400);

                string safeName = Guid.NewGuid().ToString("N") + ext;
                string uploadsDir = Path.Combine(contentRoot, "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDir);
                string fullPath = Path.Combine(uploadsDir, safeName);

                using (var stream = File.Create(fullPath))
                {
                    await file.CopyToAsync(stream);
                }

                return Results.Json(new { success = true, url = "/uploads/" + safeName });
            });

            Console.WriteLine("========================================");
            Console.WriteLine("  ME Workshop - Middle Euphrates Workshop");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("✅ Server running at: http://localhost:8080/");
            Console.WriteLine("🛠  Admin dashboard:  http://localhost:8080/admin");
            Console.WriteLine("📌 Press Ctrl+C to stop the server");
            Console.WriteLine();

            app.Run();
        }
    }

    // =============================================================================
    // ===============================  MODELS  ===================================
    // =============================================================================

    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string RegisteredAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string Status { get; set; } = "Active";
        public string LastLoginAt { get; set; } = "";
    }

    public class ServiceRequestItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public string Message { get; set; } = "";
        public string SubmittedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string Status { get; set; } = "Pending";
    }

    public class SiteContentData
    {
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public List<string> HeroImages { get; set; } = new List<string>();
    }

    public class IndustrialContentData
    {
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public string MapEmbedUrl { get; set; } = "";
    }

    public class MapPin
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameKu { get; set; } = "";
        public string AddressAr { get; set; } = "";
        public string AddressEn { get; set; } = "";
        public string AddressKu { get; set; } = "";
        public string Phone { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MapEmbedUrl { get; set; } = "";
        public bool IsApproved { get; set; } = false;
        public bool IsWorkshop { get; set; } = false;   // the workshop itself: always shown, listed first, cannot be deleted
        public string SubmittedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class ArchivalItem
    {
        public int Id { get; set; }
        public string Year { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string CaptionAr { get; set; } = "";
        public string CaptionEn { get; set; } = "";
        public string CaptionKu { get; set; } = "";
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class ExplorerCard
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "⚙️";
        public string ImageUrl { get; set; } = "";
        public string TitleAr { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string TitleKu { get; set; } = "";
        public string DescAr { get; set; } = "";
        public string DescEn { get; set; } = "";
        public string DescKu { get; set; } = "";
        public string Tags { get; set; } = "";
        public string Link { get; set; } = "/guide";
        public int SortOrder { get; set; } = 0;
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class GuideContentData
    {
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public List<GuideTopic> Topics { get; set; } = new List<GuideTopic>();
        public List<GuideEquipment> Equipment { get; set; } = new List<GuideEquipment>();
    }

    public class GuideTopic
    {
        public int Id { get; set; }
        public string TitleAr { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string TitleKu { get; set; } = "";
        public string BodyAr { get; set; } = "";
        public string BodyEn { get; set; } = "";
        public string BodyKu { get; set; } = "";
        public string Icon { get; set; } = "📘";
        public int SortOrder { get; set; } = 0;
    }

    public class GuideEquipment
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string NameKu { get; set; } = "";
        public string SpecAr { get; set; } = "";
        public string SpecEn { get; set; } = "";
        public string SpecKu { get; set; } = "";
        public string UseAr { get; set; } = "";
        public string UseEn { get; set; } = "";
        public string UseKu { get; set; } = "";
        public int SortOrder { get; set; } = 0;
    }

    // =============================================================================
    // ================================  STORE  ====================================
    // =============================================================================

    public static partial class Store
    {
        // ⚠️ AdminPassword من Environment Variable — لا hardcoded!
        private static readonly string AdminPassword = 
            System.Environment.GetEnvironmentVariable("ADMIN_PASSWORD") 
            ?? throw new InvalidOperationException(
                "❌ ADMIN_PASSWORD environment variable is required. " +
                "Set it in .env file. Refusing to start with default password.");

        private static string _dataDir = "data";
        private static readonly object UsersLock = new object();
        private static readonly object RequestsLock = new object();
        private static readonly object ContentLock = new object();
        private static readonly object IndContentLock = new object();
        private static readonly object MapPinsLock = new object();
        private static readonly object ArchivalLock = new object();
        private static readonly object ExplorerLock = new object();
        private static readonly object GuideLock = new object();

        private static List<UserAccount> _users = new List<UserAccount>();
        private static List<ServiceRequestItem> _requests = new List<ServiceRequestItem>();
        private static SiteContentData _content = new SiteContentData();
        private static IndustrialContentData _indContent = new IndustrialContentData();
        private static List<MapPin> _mapPins = new List<MapPin>();
        private static List<ArchivalItem> _archival = new List<ArchivalItem>();
        private static List<ExplorerCard> _explorerCards = new List<ExplorerCard>();
        private static GuideContentData _guideContent = new GuideContentData();

        private static readonly ConcurrentDictionary<string, DateTime> AdminTokens = new ConcurrentDictionary<string, DateTime>();

        public static readonly JsonSerializerOptions ApiJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions FileJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static string UsersPath { get { return Path.Combine(_dataDir, "users.json"); } }
        private static string RequestsPath { get { return Path.Combine(_dataDir, "service-requests.json"); } }
        private static string ContentPath { get { return Path.Combine(_dataDir, "content.json"); } }
        private static string IndustrialContentPath { get { return Path.Combine(_dataDir, "industrial-content.json"); } }
        private static string MapPinsPath { get { return Path.Combine(_dataDir, "map-pins.json"); } }
        private static string ArchivalPath { get { return Path.Combine(_dataDir, "archival-album.json"); } }
        private static string ExplorerCardsPath { get { return Path.Combine(_dataDir, "explorer-cards.json"); } }
        private static string GuideContentPath { get { return Path.Combine(_dataDir, "guide-content.json"); } }

        public static void Initialize(string contentRoot)
        {
            _dataDir = Path.Combine(contentRoot, "data");
            Directory.CreateDirectory(_dataDir);

            _users = LoadOrDefault(UsersPath, () => new List<UserAccount>());
            _requests = LoadOrDefault(RequestsPath, () => new List<ServiceRequestItem>());
            _content = LoadOrDefault(ContentPath, DefaultContent);
            _indContent = LoadOrDefault(IndustrialContentPath, DefaultIndustrialContent);
            _mapPins = LoadOrDefault(MapPinsPath, DefaultMapPins);
            _archival = LoadOrDefault(ArchivalPath, DefaultArchivalAlbum);
            _explorerCards = LoadOrDefault(ExplorerCardsPath, DefaultExplorerCards);
            _guideContent = LoadOrDefault(GuideContentPath, DefaultGuideContent);

            UpgradeLegacyData();
        }

        private static T LoadOrDefault<T>(string path, Func<T> makeDefault)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    T? obj = JsonSerializer.Deserialize<T>(json, ApiJsonOptions);
                    if (obj != null) return obj;
                }
            }
            catch
            {
                // fall through to default
            }

            T def = makeDefault();
            try { File.WriteAllText(path, JsonSerializer.Serialize(def, FileJsonOptions)); } catch { }
            return def;
        }

        private static void Persist<T>(string path, T data)
        {
            try { File.WriteAllText(path, JsonSerializer.Serialize(data, FileJsonOptions)); } catch { }
        }

        // ---------------------------------------------------------------
        // Request body helpers
        // ---------------------------------------------------------------

        public static async Task<Dictionary<string, string>> ReadJsonDict(HttpContext ctx)
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(ctx.Request.Body, ApiJsonOptions);
                return result ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static async Task<SiteContentData?> ReadSiteContent(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<SiteContentData>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<IndustrialContentData?> ReadIndustrialContent(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<IndustrialContentData>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<MapPin?> ReadMapPin(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<MapPin>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<ArchivalItem?> ReadArchivalItem(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<ArchivalItem>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<ExplorerCard?> ReadExplorerCard(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<ExplorerCard>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<GuideContentData?> ReadGuideContent(HttpContext ctx)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<GuideContentData>(ctx.Request.Body, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        // ---------------------------------------------------------------
        // Users
        // ---------------------------------------------------------------

        public static List<UserAccount> GetUsers()
        {
            lock (UsersLock)
            {
                return _users.OrderByDescending(u => u.RegisteredAt).ToList();
            }
        }

        public static Tuple<bool, string> Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Tuple.Create(false, "اسم المستخدم وكلمة المرور مطلوبان");

            lock (UsersLock)
            {
                bool exists = _users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                if (exists) return Tuple.Create(false, "اسم المستخدم مستخدم مسبقاً");

                var user = new UserAccount
                {
                    Username = username,
                    Email = email ?? "",
                    PasswordHash = Hash(password),
                    RegisteredAt = DateTime.UtcNow.ToString("o"),
                    Status = "Active",
                    LastLoginAt = ""
                };
                _users.Add(user);
                Persist(UsersPath, _users);
                return Tuple.Create(true, "");
            }
        }

        public static bool Login(string username, string password)
        {
            lock (UsersLock)
            {
                var user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                if (user == null) return false;
                if (user.PasswordHash != Hash(password)) return false;
                user.LastLoginAt = DateTime.UtcNow.ToString("o");
                Persist(UsersPath, _users);
                return true;
            }
        }

        public static bool DeleteUser(string id)
        {
            lock (UsersLock)
            {
                int removed = _users.RemoveAll(u => u.Id == int.Parse(id));
                if (removed > 0) Persist(UsersPath, _users);
                return removed > 0;
            }
        }

        // ---------------------------------------------------------------
        // Service requests
        // ---------------------------------------------------------------

        public static List<ServiceRequestItem> GetRequests()
        {
            lock (RequestsLock)
            {
                return _requests.OrderByDescending(r => r.SubmittedAt).ToList();
            }
        }

        public static ServiceRequestItem AddRequest(string fullName, string phone, string serviceType, string message)
        {
            var item = new ServiceRequestItem
            {
                FullName = fullName ?? "",
                Phone = phone ?? "",
                ServiceType = serviceType ?? "",
                Message = message ?? "",
                SubmittedAt = DateTime.UtcNow.ToString("o"),
                Status = "Pending"
            };
            lock (RequestsLock)
            {
                _requests.Add(item);
                Persist(RequestsPath, _requests);
            }
            return item;
        }

        public static bool UpdateRequestStatus(string id, string status)
        {
            lock (RequestsLock)
            {
                var req = _requests.FirstOrDefault(r => r.Id == int.Parse(id));
                if (req == null) return false;
                req.Status = status;
                Persist(RequestsPath, _requests);
                return true;
            }
        }

        public static bool DeleteRequest(string id)
        {
            lock (RequestsLock)
            {
                int removed = _requests.RemoveAll(r => r.Id == int.Parse(id));
                if (removed > 0) Persist(RequestsPath, _requests);
                return removed > 0;
            }
        }

        // ---------------------------------------------------------------
        // Content
        // ---------------------------------------------------------------

        public static SiteContentData GetContent()
        {
            lock (ContentLock) { return _content; }
        }

        public static void SaveContent(SiteContentData data)
        {
            lock (ContentLock)
            {
                _content = data;
                Persist(ContentPath, _content);
            }
        }

        public static IndustrialContentData GetIndustrialContent()
        {
            lock (IndContentLock) { return _indContent; }
        }

        public static void SaveIndustrialContent(IndustrialContentData data)
        {
            lock (IndContentLock)
            {
                _indContent = data;
                Persist(IndustrialContentPath, _indContent);
            }
        }

        // ---------------------------------------------------------------
        // Map pins
        // ---------------------------------------------------------------

        // The workshop comes first, then the rest in the order they were added.
        private static List<MapPin> Ordered(IEnumerable<MapPin> pins)
        {
            return pins.OrderByDescending(p => p.IsWorkshop).ThenBy(p => p.Id).ToList();
        }

        // Makes sure the workshop's own location exists (added once, on the first run after this update).
        private static void EnsureWorkshopPin()
        {
            if (_mapPins.Any(p => p.IsWorkshop)) return;
            _mapPins.Add(new MapPin
            {
                Id = _mapPins.Count > 0 ? _mapPins.Max(p => p.Id) + 1 : 1,
                NameAr = "ورشة الفرات الأوسط (ME Workshop)",
                NameEn = "ME Workshop — Middle Euphrates Workshop",
                NameKu = "کارگەی فوراتی ناوەڕاست (ME Workshop)",
                AddressAr = "النجف الأشرف، الحي الصناعي، منتصف شارع إعدادية الصناعة، مقابل جامعة الكوفة",
                AddressEn = "Najaf, Industrial District, middle of Industrial High School Street, opposite the University of Kufa",
                AddressKu = "نەجەف، گەڕەکی پیشەسازی، ناوەڕاستی شەقامی ئامادەیی پیشەسازی، بەرامبەر زانکۆی کووفە",
                Phone = Pages.Phones[0][1],
                Latitude = Pages.WorkshopPin[0],
                Longitude = Pages.WorkshopPin[1],
                IsApproved = true,
                IsWorkshop = true,
                SubmittedAt = DateTime.UtcNow.ToString("o")
            });
            Persist(MapPinsPath, _mapPins);
        }

        public static bool IsWorkshopPin(string id)
        {
            lock (MapPinsLock)
            {
                return int.TryParse(id, out int n) && _mapPins.Any(p => p.Id == n && p.IsWorkshop);
            }
        }

        // Basic checks so the map only ever gets sensible entries.
        public static string? ValidatePin(MapPin pin)
        {
            pin.NameAr = (pin.NameAr ?? "").Trim();
            if (pin.NameAr.Length == 0 && !string.IsNullOrWhiteSpace(pin.NameEn)) pin.NameAr = pin.NameEn.Trim();
            if (pin.NameAr.Length == 0) return "اسم الموقع مطلوب";
            if (pin.NameAr.Length > 80 || (pin.NameEn ?? "").Length > 80 || (pin.NameKu ?? "").Length > 80) return "الاسم طويل جداً";
            if ((pin.AddressAr ?? "").Length > 200 || (pin.AddressEn ?? "").Length > 200 || (pin.AddressKu ?? "").Length > 200) return "العنوان طويل جداً";
            if ((pin.Phone ?? "").Length > 20) return "رقم الهاتف غير صحيح";
            if ((pin.MapEmbedUrl ?? "").Length > 500) return "الرابط طويل جداً";
            if (double.IsNaN(pin.Latitude) || double.IsNaN(pin.Longitude) || pin.Latitude < 29 || pin.Latitude > 38 || pin.Longitude < 38 || pin.Longitude > 49)
                return "الموقع على الخريطة غير صحيح";
            return null;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<DateTime>> PinSubmits =
            new System.Collections.Concurrent.ConcurrentDictionary<string, List<DateTime>>();

        // At most 5 suggestions per hour from one address.
        public static bool AllowPinSubmission(HttpContext ctx)
        {
            string fwd = ctx.Request.Headers["X-Forwarded-For"].ToString();
            string ip = !string.IsNullOrWhiteSpace(fwd) ? fwd.Split(',')[0].Trim() : (ctx.Connection.RemoteIpAddress?.ToString() ?? "?");
            var list = PinSubmits.GetOrAdd(ip, _ => new List<DateTime>());
            lock (list)
            {
                list.RemoveAll(t => t < DateTime.UtcNow.AddHours(-1));
                if (list.Count >= 5) return false;
                list.Add(DateTime.UtcNow);
                return true;
            }
        }

        public static List<MapPin> GetApprovedMapPins()
        {
            lock (MapPinsLock)
            {
                EnsureWorkshopPin();
                return Ordered(_mapPins.Where(p => p.IsApproved));
            }
        }

        public static List<MapPin> GetPendingMapPins()
        {
            lock (MapPinsLock)
            {
                return _mapPins.Where(p => !p.IsApproved && !p.IsWorkshop).OrderByDescending(p => p.SubmittedAt).ToList();
            }
        }

        public static List<MapPin> GetAllMapPins()
        {
            lock (MapPinsLock)
            {
                EnsureWorkshopPin();
                return Ordered(_mapPins);
            }
        }

        public static MapPin AddMapPin(MapPin pin)
        {
            lock (MapPinsLock)
            {
                pin.Id = _mapPins.Count > 0 ? _mapPins.Max(p => p.Id) + 1 : 1;
                pin.NameAr = pin.NameAr ?? "";
                pin.NameEn = pin.NameEn ?? "";
                pin.NameKu = pin.NameKu ?? "";
                pin.AddressAr = pin.AddressAr ?? "";
                pin.AddressEn = pin.AddressEn ?? "";
                pin.AddressKu = pin.AddressKu ?? "";
                pin.Phone = pin.Phone ?? "";
                pin.MapEmbedUrl = pin.MapEmbedUrl ?? "";
                if (string.IsNullOrEmpty(pin.SubmittedAt)) pin.SubmittedAt = DateTime.UtcNow.ToString("o");
                _mapPins.Add(pin);
                Persist(MapPinsPath, _mapPins);
                return pin;
            }
        }

        public static bool UpdateMapPin(string id, MapPin updated)
        {
            lock (MapPinsLock)
            {
                if (!int.TryParse(id, out int pid)) return false;
                var existing = _mapPins.FirstOrDefault(p => p.Id == pid);
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
                Persist(MapPinsPath, _mapPins);
                return true;
            }
        }

        public static bool ApproveMapPin(string id)
        {
            lock (MapPinsLock)
            {
                if (!int.TryParse(id, out int pid)) return false;
                var existing = _mapPins.FirstOrDefault(p => p.Id == pid);
                if (existing == null) return false;
                existing.IsApproved = true;
                Persist(MapPinsPath, _mapPins);
                return true;
            }
        }

        public static bool DeleteMapPin(string id)
        {
            lock (MapPinsLock)
            {
                if (!int.TryParse(id, out int n)) return false;
                int removed = _mapPins.RemoveAll(p => p.Id == n && !p.IsWorkshop);
                if (removed > 0) Persist(MapPinsPath, _mapPins);
                return removed > 0;
            }
        }

        // ---------------------------------------------------------------
        // Archival album
        // ---------------------------------------------------------------

        public static List<ArchivalItem> GetArchivalAlbum()
        {
            lock (ArchivalLock)
            {
                return _archival.OrderByDescending(a => a.Year).ToList();
            }
        }

        public static ArchivalItem AddArchivalItem(ArchivalItem item)
        {
            lock (ArchivalLock)
            {
                item.Id = _archival.Count > 0 ? _archival.Max(a => a.Id) + 1 : 1;
                item.Year = item.Year ?? "";
                item.ImageUrl = item.ImageUrl ?? "";
                item.CaptionAr = item.CaptionAr ?? "";
                item.CaptionEn = item.CaptionEn ?? "";
                item.CaptionKu = item.CaptionKu ?? "";
                item.CreatedAt = DateTime.UtcNow.ToString("o");
                _archival.Add(item);
                Persist(ArchivalPath, _archival);
                return item;
            }
        }

        public static bool UpdateArchivalItem(string id, ArchivalItem updated)
        {
            lock (ArchivalLock)
            {
                var existing = _archival.FirstOrDefault(a => a.Id == int.Parse(id));
                if (existing == null) return false;
                existing.Year = updated.Year ?? "";
                existing.ImageUrl = updated.ImageUrl ?? "";
                existing.CaptionAr = updated.CaptionAr ?? "";
                existing.CaptionEn = updated.CaptionEn ?? "";
                existing.CaptionKu = updated.CaptionKu ?? "";
                Persist(ArchivalPath, _archival);
                return true;
            }
        }

        public static bool DeleteArchivalItem(string id)
        {
            lock (ArchivalLock)
            {
                int removed = _archival.RemoveAll(a => a.Id == int.Parse(id));
                if (removed > 0) Persist(ArchivalPath, _archival);
                return removed > 0;
            }
        }

        // ---------------------------------------------------------------
        // Explorer cards
        // ---------------------------------------------------------------

        public static List<ExplorerCard> GetExplorerCards()
        {
            lock (ExplorerLock)
            {
                return _explorerCards.OrderBy(c => c.SortOrder).ToList();
            }
        }

        public static ExplorerCard AddExplorerCard(ExplorerCard card)
        {
            lock (ExplorerLock)
            {
                card.Id = _explorerCards.Count > 0 ? _explorerCards.Max(c => c.Id) + 1 : 1;
                card.CreatedAt = DateTime.UtcNow.ToString("o");
                _explorerCards.Add(card);
                Persist(ExplorerCardsPath, _explorerCards);
                return card;
            }
        }

        public static bool UpdateExplorerCard(string id, ExplorerCard updated)
        {
            lock (ExplorerLock)
            {
                var existing = _explorerCards.FirstOrDefault(c => c.Id == int.Parse(id));
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
                Persist(ExplorerCardsPath, _explorerCards);
                return true;
            }
        }

        public static bool DeleteExplorerCard(string id)
        {
            lock (ExplorerLock)
            {
                int removed = _explorerCards.RemoveAll(c => c.Id == int.Parse(id));
                if (removed > 0) Persist(ExplorerCardsPath, _explorerCards);
                return removed > 0;
            }
        }

        // ---------------------------------------------------------------
        // Guide content
        // ---------------------------------------------------------------

        public static GuideContentData GetGuideContent()
        {
            lock (GuideLock) { return _guideContent; }
        }

        public static void SaveGuideContent(GuideContentData data)
        {
            lock (GuideLock)
            {
                _guideContent = data;
                Persist(GuideContentPath, _guideContent);
            }
        }

        // ---------------------------------------------------------------
        // Admin auth
        // ---------------------------------------------------------------

        public static bool CheckAdminPassword(string password)
        {
            return password == AdminPassword;
        }

        public static string CreateAdminToken()
        {
            string token = Guid.NewGuid().ToString("N");
            AdminTokens[token] = DateTime.UtcNow.AddHours(12);
            return token;
        }

        public static void RevokeAdminToken(string token)
        {
            DateTime ignored;
            AdminTokens.TryRemove(token, out ignored);
        }

        public static bool IsAdminAuthenticated(HttpContext ctx)
        {
            string? token;
            if (!ctx.Request.Cookies.TryGetValue("consan_admin", out token) || string.IsNullOrEmpty(token))
                return false;

            DateTime expiry;
            if (AdminTokens.TryGetValue(token, out expiry) && expiry > DateTime.UtcNow)
                return true;

            return false;
        }

        // ---------------------------------------------------------------
        // Hashing
        // ---------------------------------------------------------------

        private static string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ---------------------------------------------------------------
        // Default (seed) content
        // ---------------------------------------------------------------

        private static IndustrialContentData DefaultIndustrialContent()
        {
            var data = new IndustrialContentData();
            data.MapEmbedUrl = "https://www.google.com/maps?q=Industrial+Zone,+Najaf+Al-Ashraf,+Iraq&output=embed";

            data.Translations["ar"] = new Dictionary<string, string>
            {
                ["navHome"] = "الرئيسية",
                ["navAbout"] = "من نحن",
                ["navServices"] = "خدماتنا",
                ["navIndustrial"] = "خدمات صناعية",
                ["navExplorer"] = "المستكشف",
                ["navGuide"] = "الدليل الشامل",
                ["indVisitTag"] = "زوروا موقعنا",
                ["indVisitTitle"] = """موقعنا في <span class="highlight">الحي الصناعي بالنجف الأشرف</span>""",
                ["indSearchPlaceholder"] = "ابحث عن الاسم، العنوان، أو رقم الهاتف...",
                ["indColName"] = "القسم / الاسم",
                ["indColAddress"] = "العنوان",
                ["indColPhone"] = "رقم الهاتف",
                ["indHoursNote"] = "ساعات العمل: يومياً من السبت إلى الخميس، 8 صباحاً حتى 6 مساءً",
                ["indHighlightsTag"] = "خدماتنا",
                ["indHighlightsTitle"] = """ما نقدمه في <span class="highlight">ورشة الفرات الأوسط</span>""",
                ["indHighlightsSub"] = "حلول صناعية متكاملة من ورشة تعمل منذ عام 1945",
                ["indHl1Title"] = "استجابة سريعة",
                ["indHl1Desc"] = "فريق جاهز لاستقبال طلباتكم ومعالجتها بأسرع وقت ممكن",
                ["indHl2Title"] = "معدات متطورة",
                ["indHl2Desc"] = "نستخدم أحدث المخارط وأجهزة الفحص لضمان الدقة في كل عمل",
                ["indHl3Title"] = "جودة مضمونة",
                ["indHl3Desc"] = "جميع أعمالنا الصناعية مضمونة بأعلى معايير الجودة العالمية",
                ["indFeaturesTag"] = "خدماتنا الصناعية",
                ["indFeaturesTitle"] = """مجالات <span class="highlight">تخصصنا</span>""",
                ["indFeaturesSub"] = "نقدم خدمات صناعية دقيقة باستخدام أحدث المعدات والخبرات الفنية",
                ["indF1Title"] = "أعمال الخراطة الثقيلة",
                ["indF1Desc"] = "تشغيل وخراطة القطع الصناعية الثقيلة بدقة عالية باستخدام أحدث المخارط والمعدات المتخصصة",
                ["indF2Title"] = "تصنيع المعادن",
                ["indF2Desc"] = "تصميم وتصنيع القطع والهياكل المعدنية حسب الطلب لتلبية احتياجات المصانع والمعامل",
                ["indF3Title"] = "صيانة الآلات الصناعية",
                ["indF3Desc"] = "فحص وإصلاح وصيانة الآلات والمعدات الصناعية الثقيلة لضمان استمرارية الإنتاج",
                ["backHomeLink"] = "→ العودة للرئيسية",
                ["indNoResults"] = "لا توجد نتائج مطابقة",
                ["addLocationBtn"] = "إضافة موقع جديد على الخريطة",
                ["modalTitle"] = "إضافة موقع جديد",
                ["modalName"] = "اسم القسم / الورشة",
                ["modalAddress"] = "العنوان",
                ["modalPhone"] = "رقم الهاتف",
                ["modalConfirm"] = "تأكيد وإضافة الموقع",
                ["modalCancel"] = "إلغاء",
                ["submitSuccess"] = "تم إرسال موقعك بنجاح، وسيظهر في الجدول والخريطة فور موافقة الإدارة."
            };

            data.Translations["en"] = new Dictionary<string, string>
            {
                ["navHome"] = "Home",
                ["navAbout"] = "About",
                ["navServices"] = "Our Services",
                ["navIndustrial"] = "Industrial Services",
                ["navExplorer"] = "Explorer",
                ["navGuide"] = "Comprehensive Guide",
                ["indVisitTag"] = "Visit Us",
                ["indVisitTitle"] = """Our Location in the <span class="highlight">Industrial District, Najaf Al-Ashraf</span>""",
                ["indSearchPlaceholder"] = "Search by name, address, or phone number...",
                ["indColName"] = "Department / Name",
                ["indColAddress"] = "Address",
                ["indColPhone"] = "Phone Number",
                ["indHoursNote"] = "Working Hours: Daily Saturday to Thursday, 8:00 AM - 6:00 PM",
                ["indHighlightsTag"] = "Our Services",
                ["indHighlightsTitle"] = """What We Offer at <span class="highlight">Middle Euphrates Workshop</span>""",
                ["indHighlightsSub"] = "Comprehensive industrial solutions from a workshop at work since 1945",
                ["indHl1Title"] = "Fast Response",
                ["indHl1Desc"] = "A ready team to receive and process your requests as quickly as possible",
                ["indHl2Title"] = "Advanced Equipment",
                ["indHl2Desc"] = "We use the latest lathes and diagnostic tools to ensure precision in every job",
                ["indHl3Title"] = "Guaranteed Quality",
                ["indHl3Desc"] = "All our industrial work is guaranteed to the highest global quality standards",
                ["indFeaturesTag"] = "Our Industrial Services",
                ["indFeaturesTitle"] = """Areas of <span class="highlight">Expertise</span>""",
                ["indFeaturesSub"] = "We provide precise industrial services using the latest equipment and technical expertise",
                ["indF1Title"] = "Heavy Lathe Work",
                ["indF1Desc"] = "Precision machining and turning of heavy industrial parts using the latest lathes and specialized equipment",
                ["indF2Title"] = "Metal Fabrication",
                ["indF2Desc"] = "Custom design and fabrication of metal parts and structures to meet factory and plant needs",
                ["indF3Title"] = "Industrial Machinery Repair",
                ["indF3Desc"] = "Inspection, repair and maintenance of heavy industrial machinery to ensure continuous production",
                ["backHomeLink"] = "← Back to Home",
                ["indNoResults"] = "No matching results",
                ["addLocationBtn"] = "Add New Location on Map",
                ["modalTitle"] = "Add New Location",
                ["modalName"] = "Department / Workshop Name",
                ["modalAddress"] = "Address",
                ["modalPhone"] = "Phone Number",
                ["modalConfirm"] = "Confirm & Add Location",
                ["modalCancel"] = "Cancel",
                ["submitSuccess"] = "Your location has been submitted successfully and will appear on the table and map once approved by the administration."
            };

            data.Translations["ku"] = new Dictionary<string, string>
            {
                ["navHome"] = "ماڵەوە",
                ["navAbout"] = "دەربارە",
                ["navServices"] = "خزمەتەکانمان",
                ["navIndustrial"] = "خزمەتی پیشەسازی",
                ["navExplorer"] = "گەڕان",
                ["navGuide"] = "ڕێنمایی گشتگیر",
                ["indVisitTag"] = "سەردانمان بکە",
                ["indVisitTitle"] = """شوێنمان لە <span class="highlight">گەڕەکی پیشەسازی، نەجەف ئەشرەف</span>""",
                ["indSearchPlaceholder"] = "بگەڕێ بەدوای ناو، ناونیشان، یان ژمارەی مۆبایلدا...",
                ["indColName"] = "بەش / ناو",
                ["indColAddress"] = "ناونیشان",
                ["indColPhone"] = "ژمارەی مۆبایل",
                ["indHoursNote"] = "کاتەکانی کار: ڕۆژانە لە شەممە تا پێنجشەممە، 8ی بەیانی - 6ی ئێوارە",
                ["indHighlightsTag"] = "خزمەتەکانمان",
                ["indHighlightsTitle"] = """ئێمە چی پێشکەش دەکەین لە <span class="highlight">کارگەی ناوەڕاستی فورات</span>""",
                ["indHighlightsSub"] = "چارەسەری پیشەسازی گشتگیر لە کارگەیەک کە لە 1945ـەوە کار دەکات",
                ["indHl1Title"] = "وەڵامدانەوەی خێرا",
                ["indHl1Desc"] = "تیپێکی ئامادە بۆ وەرگرتن و چارەسەرکردنی داواکارییەکانتان بە خێراترین کات",
                ["indHl2Title"] = "ئامێری پێشکەوتوو",
                ["indHl2Desc"] = "نوێترین خەرادە و ئامێرەکانی پشکنین بەکاردەهێنین بۆ دڵنیابوون لە وردی",
                ["indHl3Title"] = "کوالیتی دڵنیاکراو",
                ["indHl3Desc"] = "هەموو کارە پیشەسازییەکانمان دڵنیاکراون بە بەرزترین ستانداردی جیهانی کوالیتی",
                ["indFeaturesTag"] = "خزمەتە پیشەسازییەکانمان",
                ["indFeaturesTitle"] = """بوارەکانی <span class="highlight">پسپۆڕیمان</span>""",
                ["indFeaturesSub"] = "خزمەتی پیشەسازی وردمان پێشکەش دەکەین بە بەکارهێنانی نوێترین ئامێرەکان",
                ["indF1Title"] = "کاری خەرادەی قورس",
                ["indF1Desc"] = "کارکردن و خەرادەکردنی پارچە پیشەسازییە قورسەکان بە وردی بەرز بە بەکارهێنانی نوێترین خەرادە و ئامێرە تایبەتەکان",
                ["indF2Title"] = "دروستکردنی کانزا",
                ["indF2Desc"] = "دیزاین و دروستکردنی پارچە و پێکهاتەی کانزایی بەگوێرەی داواکاری بۆ کارگە و فابریکەکان",
                ["indF3Title"] = "چاککردنەوەی ئامێری پیشەسازی",
                ["indF3Desc"] = "پشکنین و چاککردنەوە و پاراستنی ئامێر و مەکینەی پیشەسازی قورس بۆ بەردەوامی بەرهەمهێنان",
                ["backHomeLink"] = "← گەڕانەوە بۆ ماڵەوە",
                ["indNoResults"] = "هیچ ئەنجامێکی گونجاو نەدۆزرایەوە",
                ["addLocationBtn"] = "زیادکردنی شوێنی نوێ لەسەر نەخشە",
                ["modalTitle"] = "زیادکردنی شوێنی نوێ",
                ["modalName"] = "ناوی بەش / کارگە",
                ["modalAddress"] = "ناونیشان",
                ["modalPhone"] = "ژمارەی مۆبایل",
                ["modalConfirm"] = "پشتڕاستکردنەوە و زیادکردنی شوێن",
                ["modalCancel"] = "پەشیمانبوون",
                ["submitSuccess"] = "شوێنەکەت بە سەرکەوتوویی نێردرا و دوای پەسندکردنی بەڕێوەبەرایەتی لە خشتە و نەخشەکەدا دەردەکەوێت."
            };

            return data;
        }

        private static List<ExplorerCard> DefaultExplorerCards()
        {
            return new List<ExplorerCard>
            {
                new ExplorerCard
                {
                    Id = 1,
                    Icon = "⚙️",
                    ImageUrl = "https://images.unsplash.com/photo-1727292486169-33eba0865c99?w=800&h=600&fit=crop",
                    TitleAr = "ماكينات CNC المتطورة *",
                    TitleEn = "Advanced CNC Machines *",
                    TitleKu = "ئامێرەکانی CNC پێشکەوتوو *",
                    DescAr = "أنظمة خراطة وتفريز رقمية بدقة عالية لتصنيع القطع المعقدة والمخصصة بأعلى معايير الجودة العالمية.",
                    DescEn = "High-precision digital turning and milling systems for manufacturing complex and custom parts to the highest global quality standards.",
                    DescKu = "سیستەمی خەرادە و فریزکردنی دیجیتاڵی بە وردی بەرز بۆ دروستکردنی پارچە ئاڵۆز و تایبەتەکان بە بەرزترین ستانداردی جیهانی.",
                    Tags = "دقة عالية, تصنيع رقمي, تشغيل آلي",
                    Link = "/guide",
                    SortOrder = 1
                },
                new ExplorerCard
                {
                    Id = 2,
                    Icon = "🔬",
                    ImageUrl = "https://images.unsplash.com/photo-1759148413911-5ded7ed2ec06?w=800&h=600&fit=crop",
                    TitleAr = "أجهزة الفحص الإلكتروني *",
                    TitleEn = "Electronic Diagnostic Equipment *",
                    TitleKu = "ئامێرەکانی پشکنینی ئەلیکترۆنی *",
                    DescAr = "معدات تشخيص متطورة لأنظمة حقن الديزل والبنزين، مع برمجيات تحليل بيانات دقيقة لضمان أعلى مستويات الأداء.",
                    DescEn = "Advanced diagnostic equipment for diesel and gasoline injection systems, with precise data analysis software for peak performance.",
                    DescKu = "ئامێری دیاریکردنی پێشکەوتوو بۆ سیستەمی نۆزلی دیزل و بەنزین، لەگەڵ سۆفتوێری شیکاری داتای ورد بۆ بەرزترین ئاستی کارایی.",
                    Tags = "تشخيص دقيق, تحليل بيانات, قراءة أكواد",
                    Link = "/guide",
                    SortOrder = 2
                },
                new ExplorerCard
                {
                    Id = 3,
                    Icon = "💉",
                    ImageUrl = "https://images.unsplash.com/photo-1554231063-fef7ed86c0b7?w=800&h=600&fit=crop",
                    TitleAr = "أنظمة حقن الوقود *",
                    TitleEn = "Fuel Injection Systems *",
                    TitleKu = "سیستەمی نۆزلی سووتەمەنی *",
                    DescAr = "خبرة متخصصة في صيانة وإصلاح جميع أنواع أنظمة الحقن الإلكترونية والميكانيكية لأحدث المحركات.",
                    DescEn = "Specialized expertise in the maintenance and repair of all types of electronic and mechanical injection systems for the latest engines.",
                    DescKu = "پسپۆڕی تایبەت لە پاراستن و چاککردنەوەی هەموو جۆرەکانی سیستەمی نۆزلی ئەلیکترۆنی و میکانیکی بۆ نوێترین ماتۆرەکان.",
                    Tags = "ديزل, بنزين, إلكتروني",
                    Link = "/guide",
                    SortOrder = 3
                },
                new ExplorerCard
                {
                    Id = 4,
                    Icon = "🔩",
                    ImageUrl = "https://images.unsplash.com/photo-1579107821380-a2f5df32d67f?w=800&h=600&fit=crop",
                    TitleAr = "المخارط الثقيلة *",
                    TitleEn = "Heavy Lathes *",
                    TitleKu = "خەرادە قورسەکان *",
                    DescAr = "مخارط صناعية ثقيلة قادرة على تشغيل القطع الكبيرة بدقة عالية، مثالية لتصنيع قطع الغيار الصناعية.",
                    DescEn = "Heavy industrial lathes capable of machining large parts with high precision, ideal for manufacturing industrial spare parts.",
                    DescKu = "خەرادەی پیشەسازی قورس کە دەتوانێت پارچە گەورەکان بە وردی بەرز بڕێژێت، گونجاو بۆ دروستکردنی پارچەی یەدەگی پیشەسازی.",
                    Tags = "خراطة ثقيلة, قطع كبيرة, دقة متناهية",
                    Link = "/guide",
                    SortOrder = 4
                },
                new ExplorerCard
                {
                    Id = 5,
                    Icon = "✅",
                    ImageUrl = "https://images.unsplash.com/photo-1666634157070-6fd830fb5672?w=800&h=600&fit=crop",
                    TitleAr = "مختبر ضبط الجودة *",
                    TitleEn = "Quality Control Lab *",
                    TitleKu = "تاقیگەی کۆنترۆڵی کوالیتی *",
                    DescAr = "أجهزة قياس دقيقة واختبارات صارمة لضمان مطابقة كل قطعة للمواصفات الفنية المطلوبة.",
                    DescEn = "Precision measuring instruments and rigorous testing to ensure every part meets the required technical specifications.",
                    DescKu = "ئامێری پێوانەی ورد و تاقیکردنەوەی توند بۆ دڵنیابوون لەوەی هەر پارچەیەک بە تایبەتمەندییە تەکنیکییەکاندا تێدەپەڕێت.",
                    Tags = "قياس دقيق, اختبارات, ضمان الجودة",
                    Link = "/guide",
                    SortOrder = 5
                },
                new ExplorerCard
                {
                    Id = 6,
                    Icon = "🛠️",
                    ImageUrl = "https://images.unsplash.com/photo-1514443031610-8c063c7a9822?w=800&h=600&fit=crop",
                    TitleAr = "الورشة الحديثة *",
                    TitleEn = "Modern Workshop *",
                    TitleKu = "کارگەی نوێ *",
                    DescAr = "مساحة عمل مجهزة بأحدث الأدوات والمعدات، مصممة لتوفير بيئة عمل آمنة وفعالة للإنجاز السريع.",
                    DescEn = "A workspace equipped with the latest tools and equipment, designed to provide a safe and efficient work environment for rapid completion.",
                    DescKu = "شوێنی کار بە نوێترین ئامێر و کەرەستە ئامادەکراوە، دیزاین کراوە بۆ دابینکردنی ژینگەیەکی کارکردنی سەلامەت و کارا بۆ تەواوکردنی خێرا.",
                    Tags = "تنظيم, سلامة, كفاءة",
                    Link = "/guide",
                    SortOrder = 6
                }
            };
        }

        private static GuideContentData DefaultGuideContent()
        {
            var data = new GuideContentData();

            data.Translations["ar"] = new Dictionary<string, string>
            {
                ["navHome"] = "الرئيسية",
                ["navAbout"] = "من نحن",
                ["navServices"] = "خدماتنا",
                ["navIndustrial"] = "خدمات صناعية",
                ["navExplorer"] = "المستكشف",
                ["navGuide"] = "الدليل الشامل",
                ["guideTag"] = "دليل شامل",
                ["guideTitle"] = """الدليل <span class="highlight">الشامل</span>""",
                ["guideSub"] = "دليل تفصيلي عن أقسام الورشة، مواصفات المعدات، معايير السلامة، وسير العمل",
                ["guideDeptTitle"] = "أقسام الورشة",
                ["guideSpecsTitle"] = "مواصفات المعدات والآلات",
                ["guideSafetyTitle"] = "معايير السلامة المهنية",
                ["guideFlowTitle"] = "سير العمل والخدمات",
                ["guideSpecsColMachine"] = "المعدة",
                ["guideSpecsColSpec"] = "المواصفات",
                ["guideSpecsColUse"] = "الاستخدام"
            };

            data.Translations["en"] = new Dictionary<string, string>
            {
                ["navHome"] = "Home",
                ["navAbout"] = "About",
                ["navServices"] = "Our Services",
                ["navIndustrial"] = "Industrial Services",
                ["navExplorer"] = "Explorer",
                ["navGuide"] = "Comprehensive Guide",
                ["guideTag"] = "Comprehensive Guide",
                ["guideTitle"] = """The <span class="highlight">Comprehensive</span> Guide""",
                ["guideSub"] = "A detailed guide covering workshop departments, equipment specifications, safety standards, and service workflows",
                ["guideDeptTitle"] = "Workshop Departments",
                ["guideSpecsTitle"] = "Machinery & Equipment Specifications",
                ["guideSafetyTitle"] = "Occupational Safety Standards",
                ["guideFlowTitle"] = "Service Workflow",
                ["guideSpecsColMachine"] = "Machine",
                ["guideSpecsColSpec"] = "Specifications",
                ["guideSpecsColUse"] = "Usage"
            };

            data.Translations["ku"] = new Dictionary<string, string>
            {
                ["navHome"] = "ماڵەوە",
                ["navAbout"] = "دەربارە",
                ["navServices"] = "خزمەتەکانمان",
                ["navIndustrial"] = "خزمەتی پیشەسازی",
                ["navExplorer"] = "گەڕان",
                ["navGuide"] = "ڕێنمایی گشتگیر",
                ["guideTag"] = "ڕێنمایی گشتگیر",
                ["guideTitle"] = """ڕێنمایی <span class="highlight">گشتگیر</span>""",
                ["guideSub"] = "ڕێنماییەکی ورد دەربارەی بەشەکانی کارگە، تایبەتمەندی ئامێرەکان، ستانداردەکانی سەلامەتی، و پرۆسەی کارکردن",
                ["guideDeptTitle"] = "بەشەکانی کارگە",
                ["guideSpecsTitle"] = "تایبەتمەندی ئامێر و کەرەستەکان",
                ["guideSafetyTitle"] = "ستانداردەکانی سەلامەتی پیشەیی",
                ["guideFlowTitle"] = "پرۆسەی کار و خزمەتگوزاری",
                ["guideSpecsColMachine"] = "ئامێر",
                ["guideSpecsColSpec"] = "تایبەتمەندییەکان",
                ["guideSpecsColUse"] = "بەکارهێنان"
            };

            data.Topics = new List<GuideTopic>
            {
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
                    BodyKu = "پابەندین بە بەرزترین ستانداردەکانی سەلامەتی: ئامێری پاراستنی کەسی، ئاگرکوژێنەوە، نیشانەکانی ئاگادارکردنەوە، و پلانی فریاکەوتن.",
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
                    BodyKu = "شوێنپێی هەنگاوە وردەکان دەکەوین: وەرگرتنی داواکاری، پشکنین و دیاریکردن، خەمڵاندنی تێچوو، جێبەجێکردنی کار، تاقیکردنەوەی کوالیتی، پاشان گەیاندن و شوێنکەوتن.",
                    SortOrder = 3
                }
            };

            data.Equipment = new List<GuideEquipment>
            {
                new GuideEquipment
                {
                    NameAr = "ماكينة اختبار النوزلات *",
                    NameEn = "Nozzle Test Machine *",
                    NameKu = "ئامێری تاقیکردنەوەی نۆزل *",
                    SpecAr = "ضغط حتى 3000 بار، تحكم رقمي",
                    SpecEn = "Pressure up to 3000 bar, digital control",
                    SpecKu = "پەستان تا 3000 بار، کۆنترۆڵی دیجیتاڵ",
                    UseAr = "اختبار وضبط رشاشات الديزل",
                    UseEn = "Testing and calibrating diesel injectors",
                    UseKu = "تاقیکردنەوە و ڕێکخستنی ئینجێکتۆری دیزل",
                    SortOrder = 1
                },
                new GuideEquipment
                {
                    NameAr = "مخرطة CNC *",
                    NameEn = "CNC Lathe *",
                    NameKu = "خەرادەی CNC *",
                    SpecAr = "قطر 500 مم، طول 1500 مم، دقة 0.01 مم",
                    SpecEn = "Diameter 500 mm, length 1500 mm, precision 0.01 mm",
                    SpecKu = "تیرە 500 مم، درێژی 1500 مم، وردی 0.01 مم",
                    UseAr = "تصنيع قطع الغيار الدقيقة",
                    UseEn = "Manufacturing precision spare parts",
                    UseKu = "دروستکردنی پارچەی یەدەگی ورد",
                    SortOrder = 2
                },
                new GuideEquipment
                {
                    NameAr = "جهاز الفحص الإلكتروني *",
                    NameEn = "Electronic Diagnostic Device *",
                    NameKu = "ئامێری پشکنینی ئەلیکترۆنی *",
                    SpecAr = "متعدد القنوات، قراءة OBD-II",
                    SpecEn = "Multi-channel, OBD-II reading",
                    SpecKu = "فرە کەناڵ، خوێندنەوەی OBD-II",
                    UseAr = "تشخيص أعطال أنظمة الحقن",
                    UseEn = "Diagnosing injection system faults",
                    UseKu = "دیاریکردنی کێشەکانی سیستەمی نۆزل",
                    SortOrder = 3
                },
                new GuideEquipment
                {
                    NameAr = "مخرطة ثقيلة *",
                    NameEn = "Heavy Lathe *",
                    NameKu = "خەرادەی قورس *",
                    SpecAr = "قطر 1000 مم، طول 3000 مم",
                    SpecEn = "Diameter 1000 mm, length 3000 mm",
                    SpecKu = "تیرە 1000 مم، درێژی 3000 مم",
                    UseAr = "خراطة القطع الصناعية الكبيرة",
                    UseEn = "Machining large industrial parts",
                    UseKu = "خەرادەکردنی پارچە پیشەسازییە گەورەکان",
                    SortOrder = 4
                }
            };

            return data;
        }
    }

    // =============================================================================
    // ================================  PAGES  ====================================
    // =============================================================================

    public static partial class Pages
    {
        // =========================================================
        // ============   SHARED / REUSABLE BUILDING BLOCKS   ========
        // =========================================================

        public static string GetExplorerPage()
        {
            string css = GetSharedStyles() + """

            /* ===== EXPLORER PAGE STYLES ===== */
            .explorer-section {
                padding: 140px 0 80px;
                background-color: var(--bg-primary); background-image: var(--pattern-svg);
                background-repeat: repeat; background-size: 400px 400px; transition: var(--transition);
                min-height: 100vh;
            }
            .explorer-grid {
                display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 28px;
            }
            .explorer-card {
                background: var(--bg-card); border: 1px solid var(--border-gold); border-radius: 20px;
                overflow: hidden; transition: var(--transition); position: relative;
            }
            .explorer-card:hover {
                border-color: var(--gold-dark); transform: translateY(-8px);
                box-shadow: 0 20px 60px rgba(212, 175, 55, 0.15);
            }
            .explorer-card-image {
                width: 100%; height: 220px; object-fit: cover; display: block;
                filter: brightness(0.8) saturate(0.9); transition: var(--transition);
            }
            .explorer-card:hover .explorer-card-image {
                transform: scale(1.05); filter: brightness(1) saturate(1);
            }
            .explorer-card-img-wrap {
                overflow: hidden; position: relative;
            }
            .explorer-card-img-wrap::after {
                content: ''; position: absolute; inset: 0;
                background: linear-gradient(to top, var(--bg-card) 0%, transparent 60%);
                pointer-events: none;
            }
            .explorer-card-body {
                padding: 24px 26px 28px; position: relative; z-index: 1;
            }
            .explorer-card-icon {
                width: 56px; height: 56px; border-radius: 16px;
                background: var(--badge-bg); border: 1px solid var(--border-gold);
                display: flex; align-items: center; justify-content: center;
                font-size: 1.6rem; margin-bottom: 16px; transition: var(--transition);
            }
            .explorer-card:hover .explorer-card-icon {
                background: var(--gold-dark); border-color: var(--gold-dark);
            }
            .explorer-card-body h3 {
                font-size: 1.25rem; font-weight: 800; color: var(--text-primary);
                margin-bottom: 8px; transition: var(--transition);
            }
            .explorer-card-body p {
                font-size: 0.92rem; color: var(--text-secondary); line-height: 1.7;
                margin-bottom: 18px; transition: var(--transition);
            }
            .explorer-card-tags {
                display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 18px;
            }
            .explorer-tag {
                font-size: 0.7rem; font-weight: 700; padding: 4px 14px; border-radius: 50px;
                background: var(--service-tag-bg); color: var(--service-tag-color);
                border: 1px solid var(--border-gold); letter-spacing: 0.5px;
            }
            .explorer-card-link {
                display: inline-flex; align-items: center; gap: 6px;
                color: var(--gold-dark); text-decoration: none; font-weight: 700; font-size: 0.9rem;
                transition: var(--transition);
            }
            .explorer-card-link:hover { color: var(--gold-hover); gap: 10px; }

            @media (max-width: 768px) {
                .explorer-section { padding: 120px 0 60px; }
                .explorer-grid { grid-template-columns: 1fr; }
            }
            """;

            string body = $$"""
            <body>
                <div class="pattern-bg"></div>
                {{GetNavbarHtml()}}

                <section class="explorer-section">
                    <div class="container">
                        <div class="section-header">
                            <span class="section-tag" id="explorerTag">استكشف</span>
                            <h1 class="section-title" id="explorerTitle">المستكشف <span class="highlight">التقني</span></h2>
                            <p class="section-subtitle" id="explorerSub">تعرف على أحدث التقنيات والأدوات والقدرات الصناعية في ورشة الفرات الأوسط</p>
                        </div>

                        <div class="explorer-grid" id="explorerGrid"></div>
                    </div>
                </section>

                {{GetFooterHtml()}}
                {{GetContactWidgetHtml()}}

                <script>
                document.addEventListener('DOMContentLoaded', function() {
                var currentLang = 'ar';
                var explorerCards = [];

                {{GetCoreNavScript()}}
                {{GetContactWidgetScript()}}

                function escapeHtml(str) {
                    if (str === null || str === undefined) return '';
                    return String(str)
                        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                        .replace(/"/g, '&quot;').replace(/'/g, '&#039;');
                }

                function renderExplorerCards() {
                    var grid = document.getElementById('explorerGrid');
                    if (!grid) return;
                    grid.innerHTML = '';
                    explorerCards.forEach(function(card) {
                        var title = currentLang === 'en' ? (card.titleEn || card.titleAr) :
                                    currentLang === 'ku' ? (card.titleKu || card.titleAr) : (card.titleAr || card.titleEn);
                        var desc = currentLang === 'en' ? (card.descEn || card.descAr) :
                                   currentLang === 'ku' ? (card.descKu || card.descAr) : (card.descAr || card.descEn);
                        var tags = (card.tags || '').split(',').map(function(t) { return t.trim(); }).filter(function(t) { return t; });
                        var tagsHtml = tags.map(function(t) { return '<span class="explorer-tag">' + escapeHtml(t) + '</span>'; }).join('');

                        var div = document.createElement('div');
                        div.className = 'explorer-card';
                        div.innerHTML =
                            '<div class="explorer-card-img-wrap">' +
                                '<img class="explorer-card-image" src="' + escapeHtml(card.imageUrl) + '" alt="' + escapeHtml(title) + '" loading="lazy" onerror="this.style.display=\'none\'" />' +
                            '</div>' +
                            '<div class="explorer-card-body">' +
                                '<div class="explorer-card-icon">' + escapeHtml(card.icon || '⚙️') + '</div>' +
                                '<h3>' + escapeHtml(title || '') + '</h3>' +
                                '<p>' + escapeHtml(desc || '') + '</p>' +
                                '<div class="explorer-card-tags">' + tagsHtml + '</div>' +
                                '<a href="' + escapeHtml(card.link || '/guide') + '" class="explorer-card-link">' + (currentLang === 'en' ? 'Learn More →' : currentLang === 'ku' ? 'زیاتر بزانە →' : 'تعرف على المواصفات ←') + '</a>' +
                            '</div>';
                        grid.appendChild(div);
                    });
                }

                var translations = {
                    ar: {
                        navHome:'الرئيسية', navAbout:'من نحن', navServices:'خدماتنا', navIndustrial:'خدمات صناعية', navExplorer:'المستكشف', navGuide:'الدليل الشامل',
                        explorerTag:'استكشف', explorerTitle:'المستكشف <span class="highlight">التقني</span>', explorerSub:'تعرف على أحدث التقنيات والأدوات والقدرات الصناعية في ورشة الفرات الأوسط',
                        footerText:'', footerCopy:'',
                        contactTitle:'أرقام التواصل', contactLabel1:'ابو مصطفى', contactLabel2:'ابو رضا', contactLabel3:'ابو علي', contactBadgeText:'تواصل معنا'
                    },
                    en: {
                        navHome:'Home', navAbout:'About', navServices:'Our Services', navIndustrial:'Industrial Services', navExplorer:'Explorer', navGuide:'Comprehensive Guide',
                        explorerTag:'Explore', explorerTitle:'Technical <span class="highlight">Explorer</span>', explorerSub:'Discover the latest technologies, tools, and industrial capabilities at Middle Euphrates Workshop',
                        footerText:'', footerCopy:'',
                        contactTitle:'Contact Numbers', contactLabel1:'Abu Mustafa', contactLabel2:'Abu Reda', contactLabel3:'Abu Ali', contactBadgeText:'Contact Us'
                    },
                    ku: {
                        navHome:'ماڵەوە', navAbout:'دەربارە', navServices:'خزمەتەکانمان', navIndustrial:'خزمەتی پیشەسازی', navExplorer:'گەڕان', navGuide:'ڕێنمایی گشتگیر',
                        explorerTag:'بگەڕێ', explorerTitle:'گەڕانی <span class="highlight">تەکنیکی</span>', explorerSub:'نوێترین تەکنەلۆژیا و ئامێر و تواناکانی پیشەسازی لە کارگەی ناوەڕاستی فورات بناسە',
                        footerText:'', footerCopy:'',
                        contactTitle:'ژمارەکانی پەیوەندی', contactLabel1:'ئەبوو مستەفا', contactLabel2:'ئەبوو ڕەزا', contactLabel3:'ئەبوو عەلی', contactBadgeText:'پەیوەندیمان پێوەبکە'
                    }
                };

                function switchLanguage(lang) {
                    currentLang = lang;
                    document.getElementById('langOptAr').classList.toggle('active', lang === 'ar');
                    document.getElementById('langOptEn').classList.toggle('active', lang === 'en');
                    document.getElementById('langOptKu').classList.toggle('active', lang === 'ku');
                    document.getElementById('langDropdown').classList.add('closed');
                    document.getElementById('globeBtn').setAttribute('aria-expanded', 'false');
                    document.documentElement.lang = lang;
                    document.documentElement.dir = (lang === 'en') ? 'ltr' : 'rtl';
                    var t = translations[lang];
                    document.getElementById('navHome').textContent = t.navHome;
                    document.getElementById('navAbout').textContent = t.navAbout;
                    document.getElementById('navServices').textContent = t.navServices;
                    document.getElementById('navIndustrial').textContent = t.navIndustrial;
                    document.getElementById('navExplorer').textContent = t.navExplorer;
                    document.getElementById('navGuide').textContent = t.navGuide;
                    document.getElementById('explorerTag').textContent = t.explorerTag;
                    document.getElementById('explorerTitle').innerHTML = t.explorerTitle;
                    document.getElementById('explorerSub').textContent = t.explorerSub;
                    document.getElementById('footerText').textContent = t.footerText;
                    document.getElementById('footerCopy').textContent = t.footerCopy;
                    document.getElementById('contactTitle').textContent = t.contactTitle;
                    document.getElementById('contactLabel1').textContent = t.contactLabel1;
                    document.getElementById('contactLabel2').textContent = t.contactLabel2;
                    document.getElementById('contactLabel3').textContent = t.contactLabel3;
                    document.getElementById('contactBadgeText').textContent = t.contactBadgeText;
                    renderExplorerCards();
                }

                function loadExplorerCards() {
                    fetch('/api/explorer-cards').then(function(r) { return r.ok ? r.json() : []; }).then(function(cards) {
                        explorerCards = cards || [];
                        renderExplorerCards();
                    }).catch(function() { explorerCards = []; });
                }

                window.switchLanguage = switchLanguage;
                switchLanguage('ar');
                loadExplorerCards();
                });
                </script>
            </body>
            """;

            return PageShell("المستكشف | ورشة الفرات الأوسط — ME Workshop", css, body);
        }

        public static string GetGuidePage()
        {
            string css = GetSharedStyles() + """

            /* ===== GUIDE PAGE STYLES ===== */
            .guide-section {
                padding: 140px 0 80px;
                background-color: var(--bg-primary); background-image: var(--pattern-svg);
                background-repeat: repeat; background-size: 400px 400px; transition: var(--transition);
                min-height: 100vh;
            }
            .guide-container {
                max-width: 1000px; margin: 0 auto;
            }
            .guide-block {
                background: var(--bg-card); border: 1px solid var(--border-gold);
                border-radius: 20px; padding: 36px 40px; margin-bottom: 28px;
                transition: var(--transition);
            }
            .guide-block:hover {
                border-color: var(--gold-dark); box-shadow: var(--shadow-gold);
            }
            .guide-block h2 {
                font-size: 1.6rem; font-weight: 800; color: var(--text-primary);
                margin-bottom: 18px; display: flex; align-items: center; gap: 12px;
                transition: var(--transition);
            }
            .guide-block h2 .guide-icon {
                width: 44px; height: 44px; border-radius: 12px;
                background: var(--badge-bg); border: 1px solid var(--border-gold);
                display: flex; align-items: center; justify-content: center;
                font-size: 1.3rem; flex-shrink: 0;
            }
            .guide-block h3 {
                font-size: 1.15rem; font-weight: 700; color: var(--gold-dark);
                margin: 20px 0 10px; transition: var(--transition);
            }
            .guide-block p, .guide-block li {
                font-size: 0.95rem; color: var(--text-secondary); line-height: 1.8;
                transition: var(--transition);
            }
            .guide-block ul, .guide-block ol {
                padding-right: 22px; margin: 10px 0;
            }
            html[dir="ltr"] .guide-block ul, html[dir="ltr"] .guide-block ol {
                padding-right: 0; padding-left: 22px;
            }
            .guide-block li { margin-bottom: 6px; }
            .guide-block li strong { color: var(--text-primary); }

            .guide-departments-grid {
                display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; margin-top: 20px;
            }
            .guide-dept-card {
                background: var(--feature-bg); border: 1px solid var(--border-gold);
                border-radius: 14px; padding: 22px 24px; transition: var(--transition);
            }
            .guide-dept-card:hover {
                background: var(--feature-hover); border-color: var(--gold-dark);
                transform: translateY(-3px);
            }
            .guide-dept-card h4 {
                font-size: 1.05rem; font-weight: 700; color: var(--text-primary);
                margin-bottom: 8px; display: flex; align-items: center; gap: 8px;
            }
            .guide-dept-card p { font-size: 0.88rem; }
            .guide-dept-card .dept-detail {
                display: flex; justify-content: space-between; align-items: center;
                margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--border-gold);
                font-size: 0.82rem; color: var(--text-muted);
            }

            .guide-specs-table {
                width: 100%; border-collapse: collapse; margin: 16px 0;
                border-radius: 12px; overflow: hidden;
            }
            .guide-specs-table th {
                background: var(--feature-bg); color: var(--gold-dark);
                font-size: 0.78rem; font-weight: 700; padding: 12px 16px;
                text-align: right; border-bottom: 1px solid var(--border-gold);
            }
            html[dir="ltr"] .guide-specs-table th { text-align: left; }
            .guide-specs-table td {
                padding: 12px 16px; font-size: 0.88rem; color: var(--text-secondary);
                border-bottom: 1px solid var(--contact-item-border); transition: var(--transition);
            }
            .guide-specs-table tr:last-child td { border-bottom: none; }
            .guide-specs-table tr:hover td { background: var(--feature-hover); }

            .guide-safety-list {
                display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; margin-top: 16px;
            }
            .guide-safety-item {
                display: flex; gap: 12px; align-items: flex-start;
                background: var(--feature-bg); border: 1px solid var(--border-gold);
                border-radius: 12px; padding: 16px 18px; transition: var(--transition);
            }
            .guide-safety-item:hover {
                background: var(--feature-hover); border-color: var(--gold-dark);
            }
            .guide-safety-item .safety-icon {
                font-size: 1.4rem; flex-shrink: 0; line-height: 1;
            }
            .guide-safety-item h4 {
                font-size: 0.92rem; font-weight: 700; color: var(--text-primary);
                margin-bottom: 4px;
            }
            .guide-safety-item p { font-size: 0.82rem; }

            .guide-flow {
                display: flex; flex-direction: column; gap: 12px; margin-top: 18px;
            }
            .guide-flow-step {
                display: flex; gap: 16px; align-items: flex-start;
                background: var(--bg-card); border: 1px solid var(--border-gold);
                border-radius: 14px; padding: 20px 24px; transition: var(--transition);
            }
            .guide-flow-step:hover {
                border-color: var(--gold-dark); transform: translateX(6px);
                box-shadow: 0 8px 24px var(--gold-glow);
            }
            .guide-flow-num {
                width: 44px; height: 44px; border-radius: 50%; flex-shrink: 0;
                background: var(--gold-dark); color: #000; font-size: 1.2rem;
                font-weight: 900; display: flex; align-items: center; justify-content: center;
            }
            .guide-flow-step h4 {
                font-size: 1rem; font-weight: 700; color: var(--text-primary);
                margin-bottom: 4px;
            }
            .guide-flow-step p { font-size: 0.88rem; }

            @media (max-width: 768px) {
                .guide-section { padding: 120px 0 60px; }
                .guide-block { padding: 24px 20px; }
                .guide-departments-grid { grid-template-columns: 1fr; }
                .guide-safety-list { grid-template-columns: 1fr; }
                .guide-specs-table { font-size: 0.8rem; }
            }
            """;

            string body = $$"""
            <body>
                <div class="pattern-bg"></div>
                {{GetNavbarHtml()}}

                <section class="guide-section">
                    <div class="container guide-container">
                        <div class="section-header">
                            <span class="section-tag" id="guideTag">دليل شامل</span>
                            <h1 class="section-title" id="guideTitle">الدليل <span class="highlight">الشامل</span></h2>
                            <p class="section-subtitle" id="guideSub">دليل تفصيلي عن أقسام الورشة، مواصفات المعدات، معايير السلامة، وسير العمل</p>
                        </div>

                        <div id="guideTopicsContainer"></div>

                        <!-- Machinery Specifications -->
                        <div class="guide-block">
                            <h2><span class="guide-icon">🔧</span> <span id="guideSpecsTitle">مواصفات المعدات والآلات</span></h2>
                            <div class="table-wrap">
                                <table class="guide-specs-table">
                                    <thead>
                                        <tr>
                                            <th id="guideSpecsColMachine">المعدة</th>
                                            <th id="guideSpecsColSpec">المواصفات</th>
                                            <th id="guideSpecsColUse">الاستخدام</th>
                                        </tr>
                                    </thead>
                                    <tbody id="guideEquipmentBody"></tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Safety Standards -->
                        <div class="guide-block">
                            <h2><span class="guide-icon">🛡️</span> <span id="guideSafetyTitle">معايير السلامة المهنية</span></h2>
                            <div class="guide-safety-list">
                                <div class="guide-safety-item">
                                    <span class="safety-icon">🥽</span>
                                    <div>
                                        <h4 id="guideSafety1Title">معدات الحماية الشخصية</h4>
                                        <p id="guideSafety1Desc">ارتداء النظارات الواقية والقفازات وأحذية السلامة أثناء العمل على المعدات.</p>
                                    </div>
                                </div>
                                <div class="guide-safety-item">
                                    <span class="safety-icon">🧯</span>
                                    <div>
                                        <h4 id="guideSafety2Title">طفايات الحريق</h4>
                                        <p id="guideSafety2Desc">توفير طفايات حريق في جميع الأقسام مع فحص دوري وصيانة منتظمة.</p>
                                    </div>
                                </div>
                                <div class="guide-safety-item">
                                    <span class="safety-icon">⚠️</span>
                                    <div>
                                        <h4 id="guideSafety3Title">لافتات التحذير</h4>
                                        <p id="guideSafety3Desc">وضع لافتات تحذيرية واضحة في مناطق الخطر والمناطق الممنوعة.</p>
                                    </div>
                                </div>
                                <div class="guide-safety-item">
                                    <span class="safety-icon">🚨</span>
                                    <div>
                                        <h4 id="guideSafety4Title">خطة الطوارئ</h4>
                                        <p id="guideSafety4Desc">تدريب دوري على خطة الإخلاء والإسعافات الأولية لجميع العاملين.</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Service Workflow -->
                        <div class="guide-block">
                            <h2><span class="guide-icon">🔄</span> <span id="guideFlowTitle">سير العمل والخدمات</span></h2>
                            <div class="guide-flow">
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">1</div>
                                    <div>
                                        <h4 id="guideFlow1Title">استقبال الطلب</h4>
                                        <p id="guideFlow1Desc">يتم استقبال العميل وتسجيل بياناته ونوع الخدمة المطلوبة عبر النموذج الإلكتروني أو الحضور الشخصي.</p>
                                    </div>
                                </div>
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">2</div>
                                    <div>
                                        <h4 id="guideFlow2Title">الفحص والتشخيص</h4>
                                        <p id="guideFlow2Desc">يقوم الفريق الفني بفحص القطعة أو النظام باستخدام أحدث الأجهزة لتحديد المشكلة بدقة.</p>
                                    </div>
                                </div>
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">3</div>
                                    <div>
                                        <h4 id="guideFlow3Title">تقدير التكلفة</h4>
                                        <p id="guideFlow3Desc">يتم إعداد تقرير مفصل بالتكلفة والوقت المتوقع لإنجاز العمل وإرساله للعميل للموافقة.</p>
                                    </div>
                                </div>
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">4</div>
                                    <div>
                                        <h4 id="guideFlow4Title">تنفيذ العمل</h4>
                                        <p id="guideFlow4Desc">بعد الموافقة، يبدأ الفريق المختص في تنفيذ الإصلاح أو الصيانة باستخدام المعدات المناسبة.</p>
                                    </div>
                                </div>
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">5</div>
                                    <div>
                                        <h4 id="guideFlow5Title">اختبار الجودة</h4>
                                        <p id="guideFlow5Desc">يتم اختبار النتيجة النهائية والتأكد من مطابقتها للمواصفات المطلوبة قبل التسليم.</p>
                                    </div>
                                </div>
                                <div class="guide-flow-step">
                                    <div class="guide-flow-num">6</div>
                                    <div>
                                        <h4 id="guideFlow6Title">التسليم والمتابعة</h4>
                                        <p id="guideFlow6Desc">يتم تسليم العمل للعميل مع ضمان الجودة، ومتابعة ما بعد الخدمة لضمان رضاه التام.</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                {{GetFooterHtml()}}
                {{GetContactWidgetHtml()}}

                <script>
                document.addEventListener('DOMContentLoaded', function() {
                var currentLang = 'ar';
                var guideData = { translations: { ar: {}, en: {}, ku: {} }, topics: [], equipment: [] };

                {{GetCoreNavScript()}}
                {{GetContactWidgetScript()}}

                function escapeHtml(str) {
                    if (str === null || str === undefined) return '';
                    return String(str)
                        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                        .replace(/"/g, '&quot;').replace(/'/g, '&#039;');
                }

                function renderGuideTopics() {
                    var container = document.getElementById('guideTopicsContainer');
                    if (!container) return;
                    container.innerHTML = '';
                    (guideData.topics || []).forEach(function(topic) {
                        var title = currentLang === 'en' ? (topic.titleEn || topic.titleAr) :
                                    currentLang === 'ku' ? (topic.titleKu || topic.titleAr) : (topic.titleAr || topic.titleEn);
                        var body = currentLang === 'en' ? (topic.bodyEn || topic.bodyAr) :
                                   currentLang === 'ku' ? (topic.bodyKu || topic.bodyAr) : (topic.bodyAr || topic.bodyEn);
                        var div = document.createElement('div');
                        div.className = 'guide-block';
                        div.innerHTML =
                            '<h2><span class="guide-icon">' + escapeHtml(topic.icon || '📘') + '</span> <span>' + escapeHtml(title || '') + '</span></h2>' +
                            '<p>' + escapeHtml(body || '') + '</p>';
                        container.appendChild(div);
                    });
                }

                function renderGuideEquipment() {
                    var body = document.getElementById('guideEquipmentBody');
                    if (!body) return;
                    body.innerHTML = '';
                    (guideData.equipment || []).forEach(function(eq) {
                        var name = currentLang === 'en' ? (eq.nameEn || eq.nameAr) :
                                   currentLang === 'ku' ? (eq.nameKu || eq.nameAr) : (eq.nameAr || eq.nameEn);
                        var spec = currentLang === 'en' ? (eq.specEn || eq.specAr) :
                                   currentLang === 'ku' ? (eq.specKu || eq.specAr) : (eq.specAr || eq.specEn);
                        var use = currentLang === 'en' ? (eq.useEn || eq.useAr) :
                                  currentLang === 'ku' ? (eq.useKu || eq.useAr) : (eq.useAr || eq.useEn);
                        var tr = document.createElement('tr');
                        tr.innerHTML =
                            '<td>' + escapeHtml(name || '') + '</td>' +
                            '<td>' + escapeHtml(spec || '') + '</td>' +
                            '<td>' + escapeHtml(use || '') + '</td>';
                        body.appendChild(tr);
                    });
                }

                var translations = {
                    ar: {
                        navHome:'الرئيسية', navAbout:'من نحن', navServices:'خدماتنا', navIndustrial:'خدمات صناعية', navExplorer:'المستكشف', navGuide:'الدليل الشامل',
                        guideTag:'دليل شامل', guideTitle:'الدليل <span class="highlight">الشامل</span>', guideSub:'دليل تفصيلي عن أقسام الورشة، مواصفات المعدات، معايير السلامة، وسير العمل',
                        guideDeptTitle:'أقسام الورشة', guideSpecsTitle:'مواصفات المعدات والآلات', guideSafetyTitle:'معايير السلامة المهنية', guideFlowTitle:'سير العمل والخدمات',
                        guideSpecsColMachine:'المعدة', guideSpecsColSpec:'المواصفات', guideSpecsColUse:'الاستخدام',
                        footerText:'', footerCopy:'',
                        contactTitle:'أرقام التواصل', contactLabel1:'ابو مصطفى', contactLabel2:'ابو رضا', contactLabel3:'ابو علي', contactBadgeText:'تواصل معنا'
                    },
                    en: {
                        navHome:'Home', navAbout:'About', navServices:'Our Services', navIndustrial:'Industrial Services', navExplorer:'Explorer', navGuide:'Comprehensive Guide',
                        guideTag:'Comprehensive Guide', guideTitle:'The <span class="highlight">Comprehensive</span> Guide', guideSub:'A detailed guide covering workshop departments, equipment specifications, safety standards, and service workflows',
                        guideDeptTitle:'Workshop Departments', guideSpecsTitle:'Machinery & Equipment Specifications', guideSafetyTitle:'Occupational Safety Standards', guideFlowTitle:'Service Workflow',
                        guideSpecsColMachine:'Machine', guideSpecsColSpec:'Specifications', guideSpecsColUse:'Usage',
                        footerText:'', footerCopy:'',
                        contactTitle:'Contact Numbers', contactLabel1:'Abu Mustafa', contactLabel2:'Abu Reda', contactLabel3:'Abu Ali', contactBadgeText:'Contact Us'
                    },
                    ku: {
                        navHome:'ماڵەوە', navAbout:'دەربارە', navServices:'خزمەتەکانمان', navIndustrial:'خزمەتی پیشەسازی', navExplorer:'گەڕان', navGuide:'ڕێنمایی گشتگیر',
                        guideTag:'ڕێنمایی گشتگیر', guideTitle:'ڕێنمایی <span class="highlight">گشتگیر</span>', guideSub:'ڕێنماییەکی ورد دەربارەی بەشەکانی کارگە، تایبەتمەندی ئامێرەکان، ستانداردەکانی سەلامەتی، و پرۆسەی کارکردن',
                        guideDeptTitle:'بەشەکانی کارگە', guideSpecsTitle:'تایبەتمەندی ئامێر و کەرەستەکان', guideSafetyTitle:'ستانداردەکانی سەلامەتی پیشەیی', guideFlowTitle:'پرۆسەی کار و خزمەتگوزاری',
                        guideSpecsColMachine:'ئامێر', guideSpecsColSpec:'تایبەتمەندییەکان', guideSpecsColUse:'بەکارهێنان',
                        footerText:'', footerCopy:'',
                        contactTitle:'ژمارەکانی پەیوەندی', contactLabel1:'ئەبوو مستەفا', contactLabel2:'ئەبوو ڕەزا', contactLabel3:'ئەبوو عەلی', contactBadgeText:'پەیوەندیمان پێوەبکە'
                    }
                };

                function applyGuideTranslations() {
                    // Merge server translations over defaults
                    if (guideData.translations) {
                        ['ar','en','ku'].forEach(function(l) {
                            if (guideData.translations[l]) Object.assign(translations[l], guideData.translations[l]);
                        });
                    }
                }

                function switchLanguage(lang) {
                    currentLang = lang;
                    document.getElementById('langOptAr').classList.toggle('active', lang === 'ar');
                    document.getElementById('langOptEn').classList.toggle('active', lang === 'en');
                    document.getElementById('langOptKu').classList.toggle('active', lang === 'ku');
                    document.getElementById('langDropdown').classList.add('closed');
                    document.getElementById('globeBtn').setAttribute('aria-expanded', 'false');
                    document.documentElement.lang = lang;
                    document.documentElement.dir = (lang === 'en') ? 'ltr' : 'rtl';
                    var t = translations[lang];
                    document.getElementById('navHome').textContent = t.navHome;
                    document.getElementById('navAbout').textContent = t.navAbout;
                    document.getElementById('navServices').textContent = t.navServices;
                    document.getElementById('navIndustrial').textContent = t.navIndustrial;
                    document.getElementById('navExplorer').textContent = t.navExplorer;
                    document.getElementById('navGuide').textContent = t.navGuide;
                    document.getElementById('guideTag').textContent = t.guideTag;
                    document.getElementById('guideTitle').innerHTML = t.guideTitle;
                    document.getElementById('guideSub').textContent = t.guideSub;
                    document.getElementById('guideSpecsTitle').textContent = t.guideSpecsTitle;
                    document.getElementById('guideSafetyTitle').textContent = t.guideSafetyTitle;
                    document.getElementById('guideFlowTitle').textContent = t.guideFlowTitle;
                    document.getElementById('guideSpecsColMachine').textContent = t.guideSpecsColMachine;
                    document.getElementById('guideSpecsColSpec').textContent = t.guideSpecsColSpec;
                    document.getElementById('guideSpecsColUse').textContent = t.guideSpecsColUse;
                    document.getElementById('footerText').textContent = t.footerText;
                    document.getElementById('footerCopy').textContent = t.footerCopy;
                    document.getElementById('contactTitle').textContent = t.contactTitle;
                    document.getElementById('contactLabel1').textContent = t.contactLabel1;
                    document.getElementById('contactLabel2').textContent = t.contactLabel2;
                    document.getElementById('contactLabel3').textContent = t.contactLabel3;
                    document.getElementById('contactBadgeText').textContent = t.contactBadgeText;
                    renderGuideTopics();
                    renderGuideEquipment();
                }

                function loadGuideContent() {
                    fetch('/api/guide-content').then(function(r) { return r.ok ? r.json() : null; }).then(function(data) {
                        if (data) {
                            guideData = data;
                            if (!guideData.topics) guideData.topics = [];
                            if (!guideData.equipment) guideData.equipment = [];
                            applyGuideTranslations();
                        }
                        renderGuideTopics();
                        renderGuideEquipment();
                        switchLanguage(currentLang);
                    }).catch(function() { });
                }

                window.switchLanguage = switchLanguage;
                switchLanguage('ar');
                loadGuideContent();
                });
                </script>
            </body>
            """;

            return PageShell("الدليل الشامل | ورشة الفرات الأوسط — ME Workshop", css, body);
        }

        public static string GetLoginPage()
        {
            string css = GetSharedStyles();
            string body = $$"""
            <body>
                <div class="pattern-bg"></div>
                {{GetNavbarHtml()}}

                <section class="auth-section">
                    <div class="container">
                        <div class="auth-wrapper">
                            <div class="auth-header">
                                <h1 id="authTitle">تسجيل الدخول</h1>
                                <p id="authSubtitle">أهلاً بعودتك، الرجاء إدخال بياناتك</p>
                            </div>
                            <div class="auth-tabs">
                                <button class="auth-tab active" id="tabSignin" onclick="switchTab('signin')">تسجيل الدخول</button>
                                <button class="auth-tab" id="tabRegister" onclick="switchTab('register')">إنشاء حساب</button>
                            </div>

                            <form class="auth-form active" id="signinForm" onsubmit="return submitAuth(event,'signin')">
                                <div class="form-group">
                                    <label class="form-label" id="signinUserLabel">اسم المستخدم أو البريد الإلكتروني</label>
                                    <input type="text" class="form-input" id="signinUsername" required />
                                </div>
                                <div class="form-group">
                                    <label class="form-label" id="signinPassLabel">كلمة المرور</label>
                                    <input type="password" class="form-input" id="signinPassword" required />
                                </div>
                                <button type="submit" class="form-submit-btn" id="signinSubmitBtn">دخول</button>
                            </form>

                            <form class="auth-form" id="registerForm" onsubmit="return submitAuth(event,'register')">
                                <div class="form-group">
                                    <label class="form-label" id="regUserLabel">اسم المستخدم</label>
                                    <input type="text" class="form-input" id="regUsername" required />
                                </div>
                                <div class="form-group">
                                    <label class="form-label" id="regEmailLabel">البريد الإلكتروني</label>
                                    <input type="email" class="form-input" id="regEmail" required />
                                </div>
                                <div class="form-group">
                                    <label class="form-label" id="regPassLabel">كلمة المرور</label>
                                    <input type="password" class="form-input" id="regPassword" required />
                                </div>
                                <div class="form-group">
                                    <label class="form-label" id="regConfirmLabel">تأكيد كلمة المرور</label>
                                    <input type="password" class="form-input" id="regConfirmPassword" required />
                                </div>
                                <button type="submit" class="form-submit-btn" id="regSubmitBtn">إنشاء الحساب</button>
                            </form>

                            <a href="/" class="back-home-link" id="backHomeLink">→ العودة للرئيسية</a>
                        </div>
                    </div>
                </section>

                <script>
                document.addEventListener('DOMContentLoaded', function() {
                var currentLang = 'ar';
                var currentTab = 'signin';

                {{GetCoreNavScript()}}

                var translations = {
                    ar: { navHome:'الرئيسية', navAbout:'من نحن', navServices:'خدماتنا', navIndustrial:'خدمات صناعية', navExplorer:'المستكشف', navGuide:'الدليل الشامل',
                        pageTitleSignin:'تسجيل الدخول', pageTitleRegister:'إنشاء حساب جديد',
                        pageSubSignin:'أهلاً بعودتك، الرجاء إدخال بياناتك', pageSubRegister:'أنشئ حسابك للوصول إلى جميع الخدمات',
                        tabSignin:'تسجيل الدخول', tabRegister:'إنشاء حساب',
                        signinUserLabel:'اسم المستخدم أو البريد الإلكتروني', signinPassLabel:'كلمة المرور', signinSubmitBtn:'دخول',
                        regUserLabel:'اسم المستخدم', regEmailLabel:'البريد الإلكتروني', regPassLabel:'كلمة المرور', regConfirmLabel:'تأكيد كلمة المرور', regSubmitBtn:'إنشاء الحساب',
                        backHomeLink:'→ العودة للرئيسية',
                        demoAlertSignin:'تم تسجيل الدخول بنجاح', demoAlertRegister:'تم إنشاء الحساب بنجاح', mismatch:'كلمتا المرور غير متطابقتين' },
                    en: { navHome:'Home', navAbout:'About', navServices:'Our Services', navIndustrial:'Industrial Services', navExplorer:'Explorer', navGuide:'Comprehensive Guide',
                        pageTitleSignin:'Login', pageTitleRegister:'Create Account',
                        pageSubSignin:'Welcome back, please enter your details', pageSubRegister:'Create an account to access all services',
                        tabSignin:'Sign In', tabRegister:'Create Account',
                        signinUserLabel:'Username or Email', signinPassLabel:'Password', signinSubmitBtn:'Sign In',
                        regUserLabel:'Username', regEmailLabel:'Email', regPassLabel:'Password', regConfirmLabel:'Confirm Password', regSubmitBtn:'Create Account',
                        backHomeLink:'← Back to Home',
                        demoAlertSignin:'Signed in successfully', demoAlertRegister:'Account created successfully', mismatch:'Passwords do not match' },
                    ku: { navHome:'ماڵەوە', navAbout:'دەربارە', navServices:'خزمەتەکانمان', navIndustrial:'خزمەتی پیشەسازی', navExplorer:'گەڕان', navGuide:'ڕێنمایی گشتگیر',
                        pageTitleSignin:'چوونەژوورەوە', pageTitleRegister:'دروستکردنی هەژمار',
                        pageSubSignin:'بەخێربێیتەوە، تکایە زانیارییەکانت بنووسە', pageSubRegister:'هەژمارێک دروست بکە بۆ دەستگەیشتن بە هەموو خزمەتەکان',
                        tabSignin:'چوونەژوورەوە', tabRegister:'دروستکردنی هەژمار',
                        signinUserLabel:'ناوی بەکارهێنەر یان ئیمەیل', signinPassLabel:'وشەی نهێنی', signinSubmitBtn:'چوونەژوورەوە',
                        regUserLabel:'ناوی بەکارهێنەر', regEmailLabel:'ئیمەیل', regPassLabel:'وشەی نهێنی', regConfirmLabel:'دووپاتکردنەوەی وشەی نهێنی', regSubmitBtn:'دروستکردنی هەژمار',
                        backHomeLink:'← گەڕانەوە بۆ ماڵەوە',
                        demoAlertSignin:'بە سەرکەوتوویی چوویتەژوورەوە', demoAlertRegister:'هەژمار بە سەرکەوتوویی دروستکرا', mismatch:'وشەی نهێنی وەک یەک نییە' }
                };

                function switchTab(tab) {
                    currentTab = tab;
                    document.getElementById('tabSignin').classList.toggle('active', tab === 'signin');
                    document.getElementById('tabRegister').classList.toggle('active', tab === 'register');
                    document.getElementById('signinForm').classList.toggle('active', tab === 'signin');
                    document.getElementById('registerForm').classList.toggle('active', tab === 'register');
                    var t = translations[currentLang];
                    document.getElementById('authTitle').textContent = tab === 'signin' ? t.pageTitleSignin : t.pageTitleRegister;
                    document.getElementById('authSubtitle').textContent = tab === 'signin' ? t.pageSubSignin : t.pageSubRegister;
                }

                function submitAuth(e, type) {
                    e.preventDefault();
                    var t = translations[currentLang];
                    var form = e.target;

                    if (type === 'signin') {
                        var username = document.getElementById('signinUsername').value;
                        var password = document.getElementById('signinPassword').value;
                        fetch('/api/login', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ username: username, password: password })
                        }).then(function(r) {
                            return r.json().then(function(data) { return { ok: r.ok, data: data }; });
                        }).then(function(res) {
                            if (res.ok) { alert(t.demoAlertSignin); form.reset(); window.location.href = '/'; }
                            else { alert(res.data && res.data.message ? res.data.message : 'Error'); }
                        }).catch(function() { alert('Network error'); });
                    } else {
                        var rUsername = document.getElementById('regUsername').value;
                        var rEmail = document.getElementById('regEmail').value;
                        var rPassword = document.getElementById('regPassword').value;
                        var rConfirm = document.getElementById('regConfirmPassword').value;
                        if (rPassword !== rConfirm) { alert(t.mismatch); return false; }
                        fetch('/api/register', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ username: rUsername, email: rEmail, password: rPassword })
                        }).then(function(r) {
                            return r.json().then(function(data) { return { ok: r.ok, data: data }; });
                        }).then(function(res) {
                            if (res.ok) { alert(t.demoAlertRegister); form.reset(); switchTab('signin'); }
                            else { alert(res.data && res.data.message ? res.data.message : 'Error'); }
                        }).catch(function() { alert('Network error'); });
                    }
                    return false;
                }

                function switchLanguage(lang) {
                    currentLang = lang;
                    document.getElementById('langOptAr').classList.toggle('active', lang === 'ar');
                    document.getElementById('langOptEn').classList.toggle('active', lang === 'en');
                    document.getElementById('langOptKu').classList.toggle('active', lang === 'ku');
                    document.getElementById('langDropdown').classList.add('closed');
                    document.getElementById('globeBtn').setAttribute('aria-expanded', 'false');
                    document.documentElement.lang = lang;
                    document.documentElement.dir = (lang === 'en') ? 'ltr' : 'rtl';

                    var t = translations[lang];
                    document.getElementById('navHome').textContent = t.navHome;
                    document.getElementById('navAbout').textContent = t.navAbout;
                    document.getElementById('navServices').textContent = t.navServices;
                    document.getElementById('navIndustrial').textContent = t.navIndustrial;
                    document.getElementById('navExplorer').textContent = t.navExplorer;
                    document.getElementById('navGuide').textContent = t.navGuide;
                    document.getElementById('tabSignin').textContent = t.tabSignin;
                    document.getElementById('tabRegister').textContent = t.tabRegister;
                    document.getElementById('authTitle').textContent = currentTab === 'signin' ? t.pageTitleSignin : t.pageTitleRegister;
                    document.getElementById('authSubtitle').textContent = currentTab === 'signin' ? t.pageSubSignin : t.pageSubRegister;
                    document.getElementById('signinUserLabel').textContent = t.signinUserLabel;
                    document.getElementById('signinPassLabel').textContent = t.signinPassLabel;
                    document.getElementById('signinSubmitBtn').textContent = t.signinSubmitBtn;
                    document.getElementById('regUserLabel').textContent = t.regUserLabel;
                    document.getElementById('regEmailLabel').textContent = t.regEmailLabel;
                    document.getElementById('regPassLabel').textContent = t.regPassLabel;
                    document.getElementById('regConfirmLabel').textContent = t.regConfirmLabel;
                    document.getElementById('regSubmitBtn').textContent = t.regSubmitBtn;
                    document.getElementById('backHomeLink').textContent = t.backHomeLink;
                }

                window.switchTab = switchTab;
                window.submitAuth = submitAuth;
                window.switchLanguage = switchLanguage;
                });
                </script>
            </body>
            """;

            return PageShell("دخول الإدارة | ورشة الفرات الأوسط — ME Workshop", css, body);
        }

        // GetServiceRequestPage now lives in Site/ServicePage.cs

        // =========================================================
        // ============  INDUSTRIAL SERVICES PAGE (Leaflet.js)  ==========
        // =========================================================
        public static string GetIndustrialServicesPage()
        {
            string css = GetSharedStyles() + """

            /* ===== INDUSTRIAL SERVICES PAGE STYLES ===== */
            .ind-top-section {
                padding: 120px 0 60px;
                background-color: var(--bg-primary); background-image: var(--pattern-svg);
                background-repeat: repeat; background-size: 400px 400px; transition: var(--transition);
            }
            .ind-top-header { text-align: center; margin-bottom: 36px; }
            .ind-top-header .section-title { margin-bottom: 8px; }
            .ind-top-header .section-subtitle { margin: 0 auto; }

            .ind-search-wrap { max-width: 720px; margin: 0 auto 28px; position: relative; }
            .ind-search-box { display: flex; gap: 10px; align-items: stretch; }
            .ind-search-input {
                flex: 1; padding: 14px 20px; border-radius: 50px; border: 1px solid var(--border-gold);
                background: var(--bg-glass); color: var(--text-primary); font-size: 0.95rem; font-family: inherit;
                transition: var(--transition);
            }
            .ind-search-input:focus { outline: none; border-color: var(--gold-dark); box-shadow: 0 0 0 3px var(--gold-glow); }
            .ind-search-icon-btn {
                background: var(--gold-dark); color: #000; border: none; border-radius: 50px; padding: 0 28px;
                font-weight: 700; font-size: 0.9rem; cursor: pointer; font-family: inherit; transition: var(--transition);
                white-space: nowrap;
            }
            .ind-search-icon-btn:hover { background: var(--gold-hover); }

            .ind-map-grid { display: grid; grid-template-columns: 1.15fr 1fr; gap: 28px; align-items: stretch; }
            .ind-map-wrapper {
                position: relative; width: 100%; padding-top: 78%; border-radius: 16px; overflow: hidden;
                border: 1px solid var(--border-gold); box-shadow: var(--shadow-gold);
                background: var(--bg-card);
            }
            .ind-map-wrapper #map {
                position: absolute; top: 0; left: 0; width: 100%; height: 100%; border: 0;
                border-radius: 16px;
            }
            .ind-map-wrapper #map .leaflet-popup-content {
                font-family: 'Segoe UI', 'Helvetica Neue', Arial, sans-serif;
            }
            .ind-map-wrapper #map .leaflet-popup-content strong {
                color: var(--gold-dark);
            }

            .leaflet-control-attribution {
                display: none !important;
            }

            .leaflet-control-layers {
                background: var(--bg-card) !important;
                border: 1px solid var(--border-gold) !important;
                border-radius: 10px !important;
                box-shadow: 0 4px 20px rgba(0,0,0,0.15) !important;
                font-family: 'Segoe UI', 'Helvetica Neue', Arial, sans-serif !important;
            }
            .leaflet-control-layers label {
                color: var(--text-primary) !important;
                font-size: 0.85rem !important;
                padding: 4px 8px !important;
                cursor: pointer !important;
                transition: var(--transition) !important;
            }
            .leaflet-control-layers label:hover {
                background: var(--feature-hover) !important;
            }
            .leaflet-control-layers label input[type="radio"] {
                accent-color: var(--gold-dark) !important;
            }

            .ind-map-add-btn {
                position: absolute; bottom: 20px; right: 20px; z-index: 1000;
                background: var(--gold-dark); color: #000; border: none; border-radius: 50px;
                padding: 10px 20px; font-weight: 700; font-size: 0.85rem; cursor: pointer;
                font-family: inherit; transition: var(--transition);
                box-shadow: 0 4px 16px rgba(0,0,0,0.3);
            }
            .ind-map-add-btn:hover { background: var(--gold-hover); transform: scale(1.05); }

            .ind-table-panel {
                background: var(--bg-card); border: 1px solid var(--border-gold); border-radius: 16px;
                padding: 10px; display: flex; flex-direction: column; transition: var(--transition);
                max-height: 460px;
            }
            .ind-table-scroll { overflow-y: auto; overflow-x: auto; border-radius: 10px; }
            .ind-dept-table { width: 100%; border-collapse: collapse; }
            .ind-dept-table tbody tr.ws-row td { background: rgba(196,162,107,0.12); border-bottom-color: var(--accent); }
            .ind-dept-table tbody tr.ws-row td.dept-name { color: var(--accent-text); font-weight: 700; }
            .ws-tag { display: inline-block; margin-inline-end: 8px; padding: 1px 8px; border-radius: 3px; background: var(--accent); color: var(--on-brass); font-size: 0.72rem; font-weight: 700; vertical-align: middle; }
            .ind-dept-table thead th {
                position: sticky; top: 0; background: var(--feature-bg); color: var(--gold-dark);
                font-size: 0.78rem; font-weight: 700; padding: 12px 14px; text-align: right;
                border-bottom: 1px solid var(--border-gold); white-space: nowrap; z-index: 1;
            }
            html[dir="ltr"] .ind-dept-table thead th { text-align: left; }
            .ind-dept-table tbody td {
                padding: 12px 14px; font-size: 0.85rem; color: var(--text-secondary);
                border-bottom: 1px solid var(--contact-item-border); transition: var(--transition);
            }
            .ind-dept-table tbody tr { cursor: pointer; transition: var(--transition); }
            .ind-dept-table tbody tr:hover { background: var(--feature-hover); }
            .ind-dept-table tbody tr.active-row { background: var(--badge-bg); }
            .ind-dept-table tbody tr:last-child td { border-bottom: none; }
            .ind-dept-table .dept-name { font-weight: 700; color: var(--text-primary); }
            .ind-dept-table .dept-phone { direction: ltr; text-align: right; font-weight: 600; color: var(--gold-dark); }
            html[dir="ltr"] .ind-dept-table .dept-phone { text-align: left; }
            .ind-table-empty { padding: 24px 14px; text-align: center; font-size: 0.85rem; color: var(--text-muted); }
            .ind-hours-note {
                margin-top: 12px; padding: 10px 16px; font-size: 0.8rem; color: var(--text-muted);
                border-top: 1px solid var(--border-gold);
            }

            .modal-overlay {
                position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 9999;
                background: rgba(0,0,0,0.7); backdrop-filter: blur(4px);
                display: none; align-items: center; justify-content: center;
                animation: fadeIn 0.25s ease;
            }
            .modal-overlay.active { display: flex; }
            .modal-box {
                background: var(--bg-card); border-radius: 20px; max-width: 700px; width: 95%;
                max-height: 90vh; overflow-y: auto; padding: 30px;
                border: 1px solid var(--border-gold); box-shadow: 0 30px 80px rgba(0,0,0,0.5);
                position: relative;
            }
            .modal-box .modal-close {
                position: absolute; top: 14px; right: 18px; background: transparent; border: none;
                font-size: 1.8rem; cursor: pointer; color: var(--text-muted); transition: var(--transition);
                line-height: 1;
            }
            .modal-box .modal-close:hover { color: var(--text-primary); transform: rotate(90deg); }
            .modal-box h2 { color: var(--gold-dark); font-size: 1.4rem; margin-bottom: 20px; }
            .modal-map-wrap {
                position: relative; width: 100%; padding-top: 65%; border-radius: 12px;
                overflow: hidden; border: 1px solid var(--border-gold); margin-bottom: 20px;
            }
            .modal-map-wrap #modalMap {
                position: absolute; top: 0; left: 0; width: 100%; height: 100%;
                border-radius: 12px;
            }
            .modal-form-group { margin-bottom: 16px; }
            .modal-form-group label {
                display: block; font-size: 0.8rem; font-weight: 700; color: var(--gold-dark);
                margin-bottom: 4px;
            }
            .modal-form-group input {
                width: 100%; padding: 10px 14px; border-radius: 10px; border: 1px solid var(--border-gold);
                background: var(--bg-glass); color: var(--text-primary); font-size: 0.95rem;
                font-family: inherit; transition: var(--transition);
            }
            .modal-form-group input:focus { outline: none; border-color: var(--gold-dark); box-shadow: 0 0 0 3px var(--gold-glow); }
            .modal-actions { display: flex; gap: 12px; margin-top: 10px; flex-wrap: wrap; }
            .modal-actions .btn-primary { flex: 1; justify-content: center; }
            .modal-actions .btn-secondary { flex: 1; justify-content: center; }

            @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

            .ind-highlights-section {
                background-color: var(--bg-secondary); background-image: var(--pattern-svg);
                background-repeat: repeat; background-size: 400px 400px; transition: var(--transition);
            }
            .ind-highlights-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; }
            .ind-highlight-card {
                background: var(--bg-card); border: 1px solid var(--border-gold); border-radius: 16px;
                padding: 32px 26px; text-align: center; transition: var(--transition);
            }
            .ind-highlight-card:hover { border-color: var(--gold-dark); transform: translateY(-6px); box-shadow: var(--shadow-gold); }
            .ind-highlight-icon { font-size: 2.3rem; margin-bottom: 14px; }
            .ind-highlight-card h3 { font-size: 1.1rem; font-weight: 700; margin-bottom: 8px; color: var(--text-primary); transition: var(--transition); }
            .ind-highlight-card p { font-size: 0.9rem; color: var(--text-secondary); line-height: 1.7; transition: var(--transition); }

            .ind-features-section {
                background-color: var(--bg-primary); background-image: var(--pattern-svg);
                background-repeat: repeat; background-size: 400px 400px; transition: var(--transition);
            }
            .ind-features-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; }
            .ind-feature-card {
                background: var(--bg-card); border: 1px solid var(--border-gold); border-radius: 16px;
                padding: 36px 28px; text-align: center; transition: var(--transition);
            }
            .ind-feature-card:hover { border-color: var(--gold-dark); transform: translateY(-6px); box-shadow: var(--shadow-gold); }
            .ind-feature-icon { font-size: 2.6rem; margin-bottom: 16px; }
            .ind-feature-card h3 { font-size: 1.15rem; font-weight: 700; margin-bottom: 10px; color: var(--text-primary); transition: var(--transition); }
            .ind-feature-card p { font-size: 0.92rem; color: var(--text-secondary); line-height: 1.7; transition: var(--transition); }

            .ind-back-wrapper { text-align: center; margin-top: 50px; }

            @media (max-width: 1024px) {
                .ind-map-grid { grid-template-columns: 1fr; }
                .ind-table-panel { max-height: 360px; }
                .ind-highlights-grid { grid-template-columns: repeat(2, 1fr); }
                .ind-features-grid { grid-template-columns: repeat(2, 1fr); }
            }
            @media (max-width: 768px) {
                .ind-top-section { padding: 110px 0 40px; }
                .ind-highlights-grid { grid-template-columns: 1fr; }
                .ind-features-grid { grid-template-columns: 1fr; }
                .ind-search-box { flex-direction: column; }
                .modal-box { padding: 20px; }
            }
            @media (max-width: 480px) {
                .ind-map-wrapper { padding-top: 95%; }
            }
            """;

            string body = $$"""
            <body>
                <div class="pattern-bg"></div>
                {{GetNavbarHtml()}}

                <link rel="stylesheet" href="/me/leaflet/leaflet.css" />
                <script src="/me/leaflet/leaflet.js"></script>
                <script src="/me/maps.js?v=%%MAPSV%%"></script>

                <section class="ind-top-section">
                    <div class="container">
                        <div class="ind-top-header">
                            <span class="section-tag" id="indVisitTag">زوروا موقعنا</span>
                            <h1 class="section-title" id="indVisitTitle">موقعنا في <span class="highlight">الحي الصناعي بالنجف الأشرف</span></h1>
                        </div>

                        <div class="ind-search-wrap">
                            <div class="ind-search-box">
                                <input type="text" class="ind-search-input" id="indSearchInput" placeholder="ابحث عن الاسم، العنوان، أو رقم الهاتف..." autocomplete="off" />
                                <button type="button" class="ind-search-icon-btn" id="indSearchBtn">بحث</button>
                            </div>
                        </div>

                        <div class="ind-map-grid">
                            <div class="ind-map-wrapper">
                                <div id="map"></div>
                                <button class="ind-map-add-btn" id="openModalBtn" onclick="openModal()">إضافة موقع جديد</button>
                            </div>
                            <div class="ind-table-panel">
                                <div class="ind-table-scroll">
                                    <table class="ind-dept-table">
                                        <thead>
                                            <tr>
                                                <th id="indColName">القسم / الاسم</th>
                                                <th id="indColAddress">العنوان</th>
                                                <th id="indColPhone">رقم الهاتف</th>
                                            </tr>
                                        </thead>
                                        <tbody id="indDeptTableBody"></tbody>
                                    </table>
                                </div>
                                <div class="ind-hours-note" id="indHoursNote">ساعات العمل: يومياً من السبت إلى الخميس، 8 صباحاً حتى 6 مساءً</div>
                            </div>
                        </div>
                    </div>
                </section>

                <!-- Modal -->
                <div class="modal-overlay" id="modalOverlay">
                    <div class="modal-box">
                        <button class="modal-close" onclick="closeModal()">×</button>
                        <h2 id="modalTitle">إضافة موقع جديد</h2>
                        <div class="modal-map-wrap">
                            <div id="modalMap"></div>
                        </div>
                        <div class="modal-form-group">
                            <label id="modalNameLabel">اسم القسم / الورشة</label>
                            <input type="text" id="modalNameInput" placeholder="مثال: قسم تصليح البمبات" />
                        </div>
                        <div class="modal-form-group">
                            <label id="modalAddressLabel">العنوان</label>
                            <input type="text" id="modalAddressInput" placeholder="النجف الأشرف - الحي الصناعي..." />
                        </div>
                        <div class="modal-form-group">
                            <label id="modalPhoneLabel">رقم الهاتف</label>
                            <input type="text" id="modalPhoneInput" placeholder="07xx xxx xxxx" />
                        </div>
                        <div class="modal-actions">
                            <button class="btn-primary" id="modalConfirmBtn" onclick="confirmLocation()">تأكيد وإضافة الموقع</button>
                            <button class="btn-secondary" onclick="closeModal()">إلغاء</button>
                        </div>
                    </div>
                </div>

                <section class="ind-highlights-section">
                    <div class="container">
                        <div class="section-header">
                            <span class="section-tag" id="indHighlightsTag">خدماتنا</span>
                            <h2 class="section-title" id="indHighlightsTitle">ما نقدمه في <span class="highlight">ورشة الفرات الأوسط</span></h2>
                            <p class="section-subtitle" id="indHighlightsSub">حلول صناعية متكاملة من ورشة تعمل منذ عام 1945</p>
                        </div>
                        <div class="ind-highlights-grid">
                            <div class="ind-highlight-card">
                                <div class="ind-highlight-icon">⚡</div>
                                <h3 id="indHl1Title">استجابة سريعة</h3>
                                <p id="indHl1Desc">فريق جاهز لاستقبال طلباتكم ومعالجتها بأسرع وقت ممكن</p>
                            </div>
                            <div class="ind-highlight-card">
                                <div class="ind-highlight-icon">🧰</div>
                                <h3 id="indHl2Title">معدات متطورة</h3>
                                <p id="indHl2Desc">نستخدم أحدث المخارط وأجهزة الفحص لضمان الدقة في كل عمل</p>
                            </div>
                            <div class="ind-highlight-card">
                                <div class="ind-highlight-icon">✅</div>
                                <h3 id="indHl3Title">جودة مضمونة</h3>
                                <p id="indHl3Desc">جميع أعمالنا الصناعية مضمونة بأعلى معايير الجودة العالمية</p>
                            </div>
                        </div>
                    </div>
                </section>

                <section class="ind-features-section">
                    <div class="container">
                        <div class="section-header">
                            <span class="section-tag" id="indFeaturesTag">خدماتنا الصناعية</span>
                            <h2 class="section-title" id="indFeaturesTitle">مجالات <span class="highlight">تخصصنا</span></h2>
                            <p class="section-subtitle" id="indFeaturesSub">نقدم خدمات صناعية دقيقة باستخدام أحدث المعدات والخبرات الفنية</p>
                        </div>
                        <div class="ind-features-grid">
                            <div class="ind-feature-card">
                                <div class="ind-feature-icon">⚙️</div>
                                <h3 id="indF1Title">أعمال الخراطة الثقيلة</h3>
                                <p id="indF1Desc">تشغيل وخراطة القطع الصناعية الثقيلة بدقة عالية باستخدام أحدث المخارط والمعدات المتخصصة</p>
                            </div>
                            <div class="ind-feature-card">
                                <div class="ind-feature-icon">🔩</div>
                                <h3 id="indF2Title">تصنيع المعادن</h3>
                                <p id="indF2Desc">تصميم وتصنيع القطع والهياكل المعدنية حسب الطلب لتلبية احتياجات المصانع والمعامل</p>
                            </div>
                            <div class="ind-feature-card">
                                <div class="ind-feature-icon">🛠️</div>
                                <h3 id="indF3Title">صيانة الآلات الصناعية</h3>
                                <p id="indF3Desc">فحص وإصلاح وصيانة الآلات والمعدات الصناعية الثقيلة لضمان استمرارية الإنتاج</p>
                            </div>
                        </div>
                        <div class="ind-back-wrapper">
                            <a href="/" class="back-home-link" id="backHomeLink">→ العودة للرئيسية</a>
                        </div>
                    </div>
                </section>

                <script>
                document.addEventListener('DOMContentLoaded', function() {
                var currentLang = 'ar';
                var mapPins = [];
                var activePinId = null;
                var mainMap = null;
                var workshopMarker = null;
                var locateAdded = false;
                var modalMap = null;
                var modalMarker = null;
                var markers = {};
                var baseLayerGroup = null;
                var layerControl = null;

                {{GetCoreNavScript()}}

                var translations = {
                    ar: {
                        navHome:'الرئيسية', navAbout:'من نحن', navServices:'خدماتنا', navIndustrial:'خدمات صناعية', navExplorer:'المستكشف', navGuide:'الدليل الشامل',
                        indVisitTag:'زوروا موقعنا', indVisitTitle:'موقعنا في <span class="highlight">الحي الصناعي بالنجف الأشرف</span>',
                        indSearchPlaceholder:'ابحث عن الاسم، العنوان، أو رقم الهاتف...', indSearchBtn:'بحث',
                        indColName:'القسم / الاسم', indColAddress:'العنوان', indColPhone:'رقم الهاتف',
                        indHoursNote:'ساعات العمل: يومياً من السبت إلى الخميس، 8 صباحاً حتى 6 مساءً',
                        indHighlightsTag:'خدماتنا', indHighlightsTitle:'ما نقدمه في <span class="highlight">ورشة الفرات الأوسط</span>', indHighlightsSub:'حلول صناعية متكاملة من ورشة تعمل منذ عام 1945',
                        indHl1Title:'استجابة سريعة', indHl1Desc:'فريق جاهز لاستقبال طلباتكم ومعالجتها بأسرع وقت ممكن',
                        indHl2Title:'معدات متطورة', indHl2Desc:'نستخدم أحدث المخارط وأجهزة الفحص لضمان الدقة في كل عمل',
                        indHl3Title:'جودة مضمونة', indHl3Desc:'جميع أعمالنا الصناعية مضمونة بأعلى معايير الجودة العالمية',
                        indFeaturesTag:'خدماتنا الصناعية', indFeaturesTitle:'مجالات <span class="highlight">تخصصنا</span>', indFeaturesSub:'نقدم خدمات صناعية دقيقة باستخدام أحدث المعدات والخبرات الفنية',
                        indF1Title:'أعمال الخراطة الثقيلة', indF1Desc:'تشغيل وخراطة القطع الصناعية الثقيلة بدقة عالية باستخدام أحدث المخارط والمعدات المتخصصة',
                        indF2Title:'تصنيع المعادن', indF2Desc:'تصميم وتصنيع القطع والهياكل المعدنية حسب الطلب لتلبية احتياجات المصانع والمعامل',
                        indF3Title:'صيانة الآلات الصناعية', indF3Desc:'فحص وإصلاح وصيانة الآلات والمعدات الصناعية الثقيلة لضمان استمرارية الإنتاج',
                        backHomeLink:'→ العودة للرئيسية', indNoResults:'لا توجد نتائج مطابقة',
                        addLocationBtn:'إضافة موقع جديد على الخريطة',
                        modalTitle:'إضافة موقع جديد', modalName:'اسم القسم / الورشة', modalAddress:'العنوان', modalPhone:'رقم الهاتف',
                        modalConfirm:'تأكيد وإضافة الموقع', modalCancel:'إلغاء',
                        submitSuccess:'تم إرسال موقعك بنجاح، وسيظهر في الجدول والخريطة فور موافقة الإدارة.'
                    },
                    en: {
                        navHome:'Home', navAbout:'About', navServices:'Our Services', navIndustrial:'Industrial Services', navExplorer:'Explorer', navGuide:'Comprehensive Guide',
                        indVisitTag:'Visit Us', indVisitTitle:'Our Location in the <span class="highlight">Industrial District, Najaf Al-Ashraf</span>',
                        indSearchPlaceholder:'Search by name, address, or phone number...', indSearchBtn:'Search',
                        indColName:'Department / Name', indColAddress:'Address', indColPhone:'Phone Number',
                        indHoursNote:'Working Hours: Daily Saturday to Thursday, 8:00 AM - 6:00 PM',
                        indHighlightsTag:'Our Services', indHighlightsTitle:'What We Offer at <span class="highlight">Middle Euphrates Workshop</span>', indHighlightsSub:'Comprehensive industrial solutions from a workshop at work since 1945',
                        indHl1Title:'Fast Response', indHl1Desc:'A ready team to receive and process your requests as quickly as possible',
                        indHl2Title:'Advanced Equipment', indHl2Desc:'We use the latest lathes and diagnostic tools to ensure precision in every job',
                        indHl3Title:'Guaranteed Quality', indHl3Desc:'All our industrial work is guaranteed to the highest global quality standards',
                        indFeaturesTag:'Our Industrial Services', indFeaturesTitle:'Areas of <span class="highlight">Expertise</span>', indFeaturesSub:'We provide precise industrial services using the latest equipment and technical expertise',
                        indF1Title:'Heavy Lathe Work', indF1Desc:'Precision machining and turning of heavy industrial parts using the latest lathes and specialized equipment',
                        indF2Title:'Metal Fabrication', indF2Desc:'Custom design and fabrication of metal parts and structures to meet factory and plant needs',
                        indF3Title:'Industrial Machinery Repair', indF3Desc:'Inspection, repair and maintenance of heavy industrial machinery to ensure continuous production',
                        backHomeLink:'← Back to Home', indNoResults:'No matching results',
                        addLocationBtn:'Add New Location on Map',
                        modalTitle:'Add New Location', modalName:'Department / Workshop Name', modalAddress:'Address', modalPhone:'Phone Number',
                        modalConfirm:'Confirm & Add Location', modalCancel:'Cancel',
                        submitSuccess:'Your location has been submitted successfully and will appear on the table and map once approved by the administration.'
                    },
                    ku: {
                        navHome:'ماڵەوە', navAbout:'دەربارە', navServices:'خزمەتەکانمان', navIndustrial:'خزمەتی پیشەسازی', navExplorer:'گەڕان', navGuide:'ڕێنمایی گشتگیر',
                        indVisitTag:'سەردانمان بکە', indVisitTitle:'شوێنمان لە <span class="highlight">گەڕەکی پیشەسازی، نەجەف ئەشرەف</span>',
                        indSearchPlaceholder:'بگەڕێ بەدوای ناو، ناونیشان، یان ژمارەی مۆبایلدا...', indSearchBtn:'گەڕان',
                        indColName:'بەش / ناو', indColAddress:'ناونیشان', indColPhone:'ژمارەی مۆبایل',
                        indHoursNote:'کاتەکانی کار: ڕۆژانە لە شەممە تا پێنجشەممە، 8ی بەیانی - 6ی ئێوارە',
                        indHighlightsTag:'خزمەتەکانمان', indHighlightsTitle:'ئێمە چی پێشکەش دەکەین لە <span class="highlight">کارگەی ناوەڕاستی فورات</span>', indHighlightsSub:'چارەسەری پیشەسازی گشتگیر لە کارگەیەک کە لە 1945ـەوە کار دەکات',
                        indHl1Title:'وەڵامدانەوەی خێرا', indHl1Desc:'تیپێکی ئامادە بۆ وەرگرتن و چارەسەرکردنی داواکارییەکانتان بە خێراترین کات',
                        indHl2Title:'ئامێری پێشکەوتوو', indHl2Desc:'نوێترین خەرادە و ئامێرەکانی پشکنین بەکاردەهێنین بۆ دڵنیابوون لە وردی',
                        indHl3Title:'کوالیتی دڵنیاکراو', indHl3Desc:'هەموو کارە پیشەسازییەکانمان دڵنیاکراون بە بەرزترین ستانداردی جیهانی کوالیتی',
                        indFeaturesTag:'خزمەتە پیشەسازییەکانمان', indFeaturesTitle:'بوارەکانی <span class="highlight">پسپۆڕیمان</span>', indFeaturesSub:'خزمەتی پیشەسازی وردمان پێشکەش دەکەین بە بەکارهێنانی نوێترین ئامێرەکان',
                        indF1Title:'کاری خەرادەی قورس', indF1Desc:'کارکردن و خەرادەکردنی پارچە پیشەسازییە قورسەکان بە وردی بەرز بە بەکارهێنانی نوێترین خەرادە و ئامێرە تایبەتەکان',
                        indF2Title:'دروستکردنی کانزا', indF2Desc:'دیزاین و دروستکردنی پارچە و پێکهاتەی کانزایی بەگوێرەی داواکاری بۆ کارگە و فابریکەکان',
                        indF3Title:'چاککردنەوەی ئامێری پیشەسازی', indF3Desc:'پشکنین و چاککردنەوە و پاراستنی ئامێر و مەکینەی پیشەسازی قورس بۆ بەردەوامی بەرهەمهێنان',
                        backHomeLink:'← گەڕانەوە بۆ ماڵەوە', indNoResults:'هیچ ئەنجامێکی گونجاو نەدۆزرایەوە',
                        addLocationBtn:'زیادکردنی شوێنی نوێ لەسەر نەخشە',
                        modalTitle:'زیادکردنی شوێنی نوێ', modalName:'ناوی بەش / کارگە', modalAddress:'ناونیشان', modalPhone:'ژمارەی مۆبایل',
                        modalConfirm:'پشتڕاستکردنەوە و زیادکردنی شوێن', modalCancel:'پەشیمانبوون',
                        submitSuccess:'شوێنەکەت بە سەرکەوتوویی نێردرا و دوای پەسندکردنی بەڕێوەبەرایەتی لە خشتە و نەخشەکەدا دەردەکەوێت.'
                    }
                };

                // ---- Google Maps Layers ----
                function createGoogleLayer(layerType, label) {
                    return MEMaps.layer(layerType, label);
                }

                var googleRoadmap = createGoogleLayer('m', 'خريطة الشوارع');
                var googleSatellite = createGoogleLayer('s', 'الصور الفضائية / الأقمار الصناعية');
                var googleHybrid = createGoogleLayer('y', 'الخريطة الهجينة');
                var googleTerrain = createGoogleLayer('p', 'التضاريس');

                // ---- Leaflet initialization ----
                function initMainMap() {
                    if (mainMap) return;
                    mainMap = L.map('map', {
                        center: MEMaps.center,
                        zoom: 16,
                        layers: [googleRoadmap]
                    });

                    var baseMaps = {
                        'خريطة الشوارع': googleRoadmap,
                        'الصور الفضائية': googleSatellite,
                    };
                    layerControl = L.control.layers(baseMaps, null, {
                        position: 'topright',
                        collapsed: true
                    }).addTo(mainMap);

                    baseLayerGroup = L.layerGroup().addTo(mainMap);
                }

                function initModalMap() {
                    if (modalMap) return;
                    modalMap = L.map('modalMap', {
                        center: MEMaps.center,
                        zoom: 16,
                        layers: [googleRoadmap]
                    });

                    var baseMapsModal = {
                        'خريطة الشوارع': googleRoadmap,
                        'الصور الفضائية': googleSatellite,
                    };
                    L.control.layers(baseMapsModal, null, {
                        position: 'topright',
                        collapsed: true
                    }).addTo(modalMap);

                    modalMap.on('click', function(e) {
                        if (modalMarker) {
                            modalMarker.setLatLng(e.latlng);
                        } else {
                            modalMarker = L.marker(e.latlng, { draggable: true }).addTo(modalMap);
                        }
                        modalMap.setView(e.latlng, 16);
                    });
                }

                // ---- Map pin management ----
                function pinDisplayName(pin) {
                    if (currentLang === 'en') return pin.nameEn || pin.nameAr || '';
                    if (currentLang === 'ku') return pin.nameKu || pin.nameAr || '';
                    return pin.nameAr || pin.nameEn || '';
                }

                function pinDisplayAddress(pin) {
                    if (currentLang === 'en') return pin.addressEn || pin.addressAr || '';
                    if (currentLang === 'ku') return pin.addressKu || pin.addressAr || '';
                    return pin.addressAr || pin.addressEn || '';
                }

                function addMarkerToMap(pin) {
                    if (!mainMap) return;
                    if (markers[pin.id]) {
                        markers[pin.id].remove();
                        delete markers[pin.id];
                    }
                    var popupContent = '<strong>' + pinDisplayName(pin) + '</strong><br>' + pinDisplayAddress(pin) + '<br>' + (pin.phone || '-');
                    var marker;
                    if (pin.isWorkshop) {
                        // the workshop: distinct badge, permanent label, directions, always on top
                        marker = MEMaps.addWorkshop(baseLayerGroup, pin.latitude, pin.longitude, pinDisplayName(pin),
                            '<span>' + pinDisplayAddress(pin) + '</span><br><span dir="ltr">' + (pin.phone || '') + '</span><br>');
                        workshopMarker = marker;
                    } else {
                        marker = L.marker([pin.latitude, pin.longitude])
                            .addTo(baseLayerGroup)
                            .bindPopup(popupContent);
                    }
                    markers[pin.id] = marker;
                    if (pin.id === activePinId) {
                        marker.openPopup();
                        mainMap.setView([pin.latitude, pin.longitude], 16);
                    }
                }

                function renderTable(list) {
                    var body = document.getElementById('indDeptTableBody');
                    body.innerHTML = '';
                    if (!list || !list.length) {
                        var t = translations[currentLang];
                        var tr = document.createElement('tr');
                        var td = document.createElement('td');
                        td.colSpan = 3;
                        td.className = 'ind-table-empty';
                        td.textContent = t.indNoResults;
                        tr.appendChild(td);
                        body.appendChild(tr);
                        return;
                    }
                    list.forEach(function(pin) {
                        var tr = document.createElement('tr');
                        tr.dataset.pinId = pin.id;
                        if (pin.id === activePinId) tr.classList.add('active-row');

                        var tdName = document.createElement('td');
                        tdName.className = 'dept-name';
                        tdName.textContent = pinDisplayName(pin);

                        var tdAddr = document.createElement('td');
                        tdAddr.textContent = pinDisplayAddress(pin);

                        var tdPhone = document.createElement('td');
                        tdPhone.className = 'dept-phone';
                        tdPhone.textContent = pin.phone || '-';

                        if (pin.isWorkshop) {
                            tr.classList.add('ws-row');
                            var tag = document.createElement('span');
                            tag.className = 'ws-tag';
                            tag.textContent = currentLang === 'en' ? 'Workshop' : (currentLang === 'ku' ? 'کارگە' : 'الورشة');
                            tdName.prepend(tag);
                        }
                        tr.appendChild(tdName);
                        tr.appendChild(tdAddr);
                        tr.appendChild(tdPhone);
                        tr.addEventListener('click', function() { selectPin(pin); });
                        body.appendChild(tr);
                    });
                }

                function selectPin(pin) {
                    activePinId = pin.id;
                    document.querySelectorAll('#indDeptTableBody tr').forEach(function(tr) {
                        tr.classList.toggle('active-row', tr.dataset.pinId === String(pin.id));
                    });
                    if (mainMap && pin.latitude && pin.longitude) {
                        mainMap.flyTo([pin.latitude, pin.longitude], 16, { duration: 1.2 });
                        if (markers[pin.id]) {
                            markers[pin.id].openPopup();
                        }
                    }
                }

                function runFilter() {
                    var q = document.getElementById('indSearchInput').value.trim().toLowerCase();
                    var list = mapPins;
                    if (q) {
                        list = mapPins.filter(function(pin) {
                            var name = (pin.nameAr || '') + ' ' + (pin.nameEn || '') + ' ' + (pin.nameKu || '');
                            var addr = (pin.addressAr || '') + ' ' + (pin.addressEn || '') + ' ' + (pin.addressKu || '');
                            return name.toLowerCase().indexOf(q) !== -1 ||
                                   addr.toLowerCase().indexOf(q) !== -1 ||
                                   (pin.phone || '').toLowerCase().indexOf(q) !== -1;
                        });
                    }
                    renderTable(list);
                }

                function loadMapPins() {
                    fetch('/api/map-pins').then(function(r) { return r.ok ? r.json() : []; }).then(function(pins) {
                        mapPins = pins || [];
                        Object.keys(markers).forEach(function(id) {
                            markers[id].remove();
                            delete markers[id];
                        });
                        mapPins.forEach(function(pin) {
                            addMarkerToMap(pin);
                        });
                        renderTable(mapPins);
                        // Open on the workshop (the server always lists it first) with its card showing.
                        var ws = mapPins.filter(function(p) { return p.isWorkshop; })[0] || mapPins[0];
                        if (ws && mainMap) {
                            if (!activePinId) activePinId = ws.id;
                            mainMap.setView([ws.latitude, ws.longitude], 17);
                            if (markers[ws.id]) markers[ws.id].openPopup();
                            document.querySelectorAll('#indDeptTableBody tr').forEach(function(tr) { tr.classList.toggle('active-row', tr.dataset.pinId === String(ws.id)); });
                        }
                        if (mainMap && !locateAdded) { locateAdded = true; MEMaps.locateControl(mainMap, function() { return workshopMarker; }); }
                    }).catch(function() { mapPins = []; renderTable(mapPins); });
                }

                // ---- Modal functions ----
                window.openModal = function() {
                    var overlay = document.getElementById('modalOverlay');
                    overlay.classList.add('active');
                    document.body.style.overflow = 'hidden';
                    setTimeout(function() {
                        initModalMap();
                        if (modalMap) {
                            modalMap.invalidateSize();
                            modalMap.setView(MEMaps.center, 16);
                            if (modalMarker) {
                                modalMarker.remove();
                                modalMarker = null;
                            }
                            // Force synthetic resize for Leaflet tiles
                            window.dispatchEvent(new Event('resize'));
                        }
                        document.getElementById('modalNameInput').value = '';
                        document.getElementById('modalAddressInput').value = '';
                        document.getElementById('modalPhoneInput').value = '';
                    }, 200);
                };

                window.closeModal = function() {
                    document.getElementById('modalOverlay').classList.remove('active');
                    document.body.style.overflow = '';
                };

                window.confirmLocation = function() {
                    var nameAr = document.getElementById('modalNameInput').value.trim();
                    var addressAr = document.getElementById('modalAddressInput').value.trim();
                    var phone = document.getElementById('modalPhoneInput').value.trim();

                    if (!modalMarker) {
                        alert('الرجاء النقر على الخريطة لتحديد موقع القسم');
                        return;
                    }
                    if (!nameAr) {
                        alert('الرجاء إدخال اسم القسم');
                        return;
                    }

                    var latlng = modalMarker.getLatLng();
                    var payload = {
                        nameAr: nameAr,
                        nameEn: nameAr,
                        nameKu: nameAr,
                        addressAr: addressAr || 'النجف الأشرف - الحي الصناعي',
                        addressEn: addressAr || 'Najaf Al-Ashraf - Industrial District',
                        addressKu: addressAr || 'نەجەف ئەشرەف - گەڕەکی پیشەسازی',
                        phone: phone || '',
                        latitude: latlng.lat,
                        longitude: latlng.lng,
                        mapEmbedUrl: ''
                    };

                    fetch('/api/map-pins/submit', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) {
                            closeModal();
                            alert(translations[currentLang].submitSuccess);
                            loadMapPins();
                        } else {
                            alert('حدث خطأ أثناء إضافة الموقع');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء إضافة الموقع'); });
                };

                // ---- Search events ----
                document.getElementById('indSearchBtn').addEventListener('click', runFilter);
                document.getElementById('indSearchInput').addEventListener('input', runFilter);
                document.getElementById('indSearchInput').addEventListener('keydown', function(e) {
                    if (e.key === 'Enter') { e.preventDefault(); runFilter(); }
                });

                // ---- Language switching ----
                function switchLanguage(lang) {
                    currentLang = lang;
                    document.getElementById('langOptAr').classList.toggle('active', lang === 'ar');
                    document.getElementById('langOptEn').classList.toggle('active', lang === 'en');
                    document.getElementById('langOptKu').classList.toggle('active', lang === 'ku');
                    document.getElementById('langDropdown').classList.add('closed');
                    document.getElementById('globeBtn').setAttribute('aria-expanded', 'false');
                    document.documentElement.lang = lang;
                    document.documentElement.dir = (lang === 'en') ? 'ltr' : 'rtl';

                    var t = translations[lang];
                    document.getElementById('navHome').textContent = t.navHome;
                    document.getElementById('navAbout').textContent = t.navAbout;
                    document.getElementById('navServices').textContent = t.navServices;
                    document.getElementById('navIndustrial').textContent = t.navIndustrial;
                    document.getElementById('navExplorer').textContent = t.navExplorer;
                    document.getElementById('navGuide').textContent = t.navGuide;

                    document.getElementById('indVisitTag').textContent = t.indVisitTag;
                    document.getElementById('indVisitTitle').innerHTML = t.indVisitTitle;
                    document.getElementById('indSearchInput').placeholder = t.indSearchPlaceholder;
                    document.getElementById('indSearchBtn').textContent = t.indSearchBtn;
                    document.getElementById('indColName').textContent = t.indColName;
                    document.getElementById('indColAddress').textContent = t.indColAddress;
                    document.getElementById('indColPhone').textContent = t.indColPhone;
                    document.getElementById('indHoursNote').textContent = t.indHoursNote;

                    document.getElementById('indHighlightsTag').textContent = t.indHighlightsTag;
                    document.getElementById('indHighlightsTitle').innerHTML = t.indHighlightsTitle;
                    document.getElementById('indHighlightsSub').textContent = t.indHighlightsSub;
                    document.getElementById('indHl1Title').textContent = t.indHl1Title;
                    document.getElementById('indHl1Desc').textContent = t.indHl1Desc;
                    document.getElementById('indHl2Title').textContent = t.indHl2Title;
                    document.getElementById('indHl2Desc').textContent = t.indHl2Desc;
                    document.getElementById('indHl3Title').textContent = t.indHl3Title;
                    document.getElementById('indHl3Desc').textContent = t.indHl3Desc;

                    document.getElementById('indFeaturesTag').textContent = t.indFeaturesTag;
                    document.getElementById('indFeaturesTitle').innerHTML = t.indFeaturesTitle;
                    document.getElementById('indFeaturesSub').textContent = t.indFeaturesSub;
                    document.getElementById('indF1Title').textContent = t.indF1Title;
                    document.getElementById('indF1Desc').textContent = t.indF1Desc;
                    document.getElementById('indF2Title').textContent = t.indF2Title;
                    document.getElementById('indF2Desc').textContent = t.indF2Desc;
                    document.getElementById('indF3Title').textContent = t.indF3Title;
                    document.getElementById('indF3Desc').textContent = t.indF3Desc;

                    document.getElementById('backHomeLink').textContent = t.backHomeLink;

                    document.getElementById('modalTitle').textContent = t.modalTitle;
                    document.getElementById('modalNameLabel').textContent = t.modalName;
                    document.getElementById('modalAddressLabel').textContent = t.modalAddress;
                    document.getElementById('modalPhoneLabel').textContent = t.modalPhone;
                    document.getElementById('modalConfirmBtn').textContent = t.modalConfirm;

                    if (layerControl) {
                        var baseMaps = {};
                        if (lang === 'ar') {
                            baseMaps['خريطة الشوارع'] = googleRoadmap;
                            baseMaps['الصور الفضائية'] = googleSatellite;
                            baseMaps['الخريطة الهجينة'] = googleHybrid;
                            baseMaps['التضاريس'] = googleTerrain;
                        } else if (lang === 'en') {
                            baseMaps['Google Maps'] = googleRoadmap;
                            baseMaps['Satellite'] = googleSatellite;
                            baseMaps['Hybrid'] = googleHybrid;
                            baseMaps['Terrain'] = googleTerrain;
                        } else {
                            baseMaps['نەخشەی جۆجڵ'] = googleRoadmap;
                            baseMaps['وێنەی مانگا دەستکرد'] = googleSatellite;
                            baseMaps['نەخشەی تێکەڵ'] = googleHybrid;
                            baseMaps['زەوی'] = googleTerrain;
                        }
                        mainMap.removeControl(layerControl);
                        layerControl = L.control.layers(baseMaps, null, {
                            position: 'topright',
                            collapsed: true
                        }).addTo(mainMap);
                    }

                    renderTable(mapPins.length ? filteredOrAll() : mapPins);

                    if (mainMap) {
                        mapPins.forEach(function(pin) {
                            if (markers[pin.id]) {
                                var popupContent = '<strong>' + pinDisplayName(pin) + '</strong><br>' + pinDisplayAddress(pin) + '<br>' + (pin.phone || '-');
                                markers[pin.id].bindPopup(popupContent);
                            }
                        });
                    }
                }

                function filteredOrAll() {
                    var q = document.getElementById('indSearchInput').value.trim().toLowerCase();
                    if (!q) return mapPins;
                    return mapPins.filter(function(pin) {
                        var name = (pin.nameAr || '') + ' ' + (pin.nameEn || '') + ' ' + (pin.nameKu || '');
                        var addr = (pin.addressAr || '') + ' ' + (pin.addressEn || '') + ' ' + (pin.addressKu || '');
                        return name.toLowerCase().indexOf(q) !== -1 ||
                               addr.toLowerCase().indexOf(q) !== -1 ||
                               (pin.phone || '').toLowerCase().indexOf(q) !== -1;
                    });
                }

                window.switchLanguage = switchLanguage;

                // ---- Init ----
                initMainMap();
                loadMapPins();

                document.getElementById('modalOverlay').addEventListener('click', function(e) {
                    if (e.target === this) closeModal();
                });

                document.addEventListener('keydown', function(e) {
                    if (e.key === 'Escape') closeModal();
                });

                fetch('/api/industrial-content').then(function(r) { return r.ok ? r.json() : null; }).then(function(data) {
                    if (data && data.translations) {
                        ['ar', 'en', 'ku'].forEach(function(l) {
                            if (data.translations[l]) Object.assign(translations[l], data.translations[l]);
                        });
                    }
                    switchLanguage(currentLang);
                }).catch(function() { });
                });
                </script>
                {{GetContactWidgetHtml()}}
            </body>
            """;

            return PageShell("الخدمات الصناعية | ورشة الفرات الأوسط — ME Workshop", css, body.Replace("%%MAPSV%%", AssetVersion));
        }

        // =========================================================
        // ===========================  ADMIN  ========================
        // =========================================================

        public static string GetAdminPage()
        {
            string css = """
            * { margin: 0; padding: 0; box-sizing: border-box; }
            html { scroll-behavior: smooth; }
            body {
                font-family: 'Segoe UI', 'Helvetica Neue', Arial, sans-serif;
                background: #0f0f10; color: #f1f1f1; min-height: 100vh; line-height: 1.6;
            }
            a { color: inherit; }
            .hidden { display: none !important; }

            /* ---- Login gate ---- */
            #loginGate { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 20px; }
            .login-box {
                width: 100%; max-width: 380px; background: #1a1a1a; border: 1px solid rgba(212,175,55,0.3);
                border-radius: 16px; padding: 36px 30px; text-align: center; box-shadow: 0 20px 60px rgba(0,0,0,0.4);
            }
            .login-box h2 { font-size: 1.3rem; margin-bottom: 22px; color: #D4AF37; }
            .login-box input {
                width: 100%; padding: 12px 14px; border-radius: 10px; border: 1px solid rgba(212,175,55,0.3);
                background: #111; color: #fff; font-size: 0.95rem; margin-bottom: 14px; font-family: inherit;
            }
            .login-box input:focus { outline: none; border-color: #D4AF37; }
            .login-box button {
                width: 100%; padding: 12px; border: none; border-radius: 10px; background: #D4AF37; color: #000;
                font-weight: 700; font-size: 0.95rem; cursor: pointer; font-family: inherit;
            }
            .login-box button:hover { background: #C0951C; }
            .error-text { color: #ff6b6b; font-size: 0.85rem; margin-top: 12px; min-height: 18px; }

            /* ---- Dashboard shell ---- */
            .admin-header {
                display: flex; justify-content: space-between; align-items: center; padding: 18px 28px;
                background: #161616; border-bottom: 1px solid rgba(212,175,55,0.25); position: sticky; top: 0; z-index: 10;
                flex-wrap: wrap; gap: 12px;
            }
            .admin-header h1 { font-size: 1.15rem; color: #D4AF37; }
            .admin-header .actions { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }

            #headerAccountInfo { transition: all 0.3s ease; }

            @media (max-width: 1100px) {
                #headerAccountInfo {
                    width: 100%;
                    justify-content: center;
                    order: -1;
                }
            }
            .admin-header a, .admin-header button {
                font-size: 0.85rem; padding: 8px 16px; border-radius: 8px; text-decoration: none;
                border: 1px solid rgba(212,175,55,0.3); background: transparent; color: #f1f1f1; cursor: pointer; font-family: inherit;
            }
            .admin-header a:hover, .admin-header button:hover { border-color: #D4AF37; color: #D4AF37; }

            /* ===== Visitors ===== */
            .vis-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 16px; }
            .vis-head h2 { font-size: 1.15rem; color: #eee; }
            .vis-range { display: flex; gap: 6px; }
            .vis-range button { background: #161616; border: 1px solid #333; color: #bbb; border-radius: 8px; padding: 7px 14px; font: inherit; font-size: 0.82rem; cursor: pointer; }
            .vis-range button.active { border-color: #D4AF37; color: #D4AF37; }
            .vis-cards { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
            .vis-card { background: #161616; border: 1px solid #2a2a2a; border-radius: 12px; padding: 16px 18px; display: grid; gap: 4px; }
            .vis-card span { color: #999; font-size: 0.8rem; }
            .vis-card b { font-size: 1.9rem; color: #fff; font-variant-numeric: tabular-nums; direction: ltr; text-align: start; }
            .vis-card small { color: #D4AF37; font-size: 0.78rem; }
            .vis-chart { display: flex; align-items: flex-end; gap: 3px; height: 170px; padding-top: 10px; direction: ltr; }
            .vis-bar { flex: 1 1 0; min-width: 3px; background: rgba(212,175,55,0.25); border-radius: 3px 3px 0 0; position: relative; }
            .vis-bar i { position: absolute; inset: auto 0 0 0; background: #D4AF37; border-radius: 3px 3px 0 0; }
            .vis-bar:hover { background: rgba(212,175,55,0.4); }
            .vis-axis { display: flex; justify-content: space-between; color: #777; font-size: 0.72rem; margin-top: 6px; direction: ltr; }
            .vis-legend { color: #888; font-size: 0.75rem; margin-top: 4px; }
            .vis-legend i { display: inline-block; width: 10px; height: 10px; border-radius: 2px; vertical-align: middle; margin-inline: 8px 4px; }
            .vis-tops { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; margin-top: 20px; }
            .vis-list { list-style: none; display: grid; gap: 8px; }
            .vis-list li { display: grid; grid-template-columns: 1fr auto; gap: 4px 10px; font-size: 0.84rem; color: #ddd; }
            .vis-list li span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; direction: ltr; text-align: start; unicode-bidi: plaintext; }
            .vis-list li b { color: #D4AF37; font-variant-numeric: tabular-nums; }
            .vis-list li em { grid-column: 1 / -1; height: 4px; background: #222; border-radius: 4px; overflow: hidden; display: block; }
            .vis-list li em s { display: block; height: 100%; background: #D4AF37; text-decoration: none; }
            .vis-note { color: #777; font-size: 0.78rem; margin-top: 16px; line-height: 1.8; }
            .vis-mono { direction: ltr; unicode-bidi: isolate; font-family: ui-monospace, Menlo, monospace; font-size: 0.8rem; }
            @media (max-width: 1100px) { .vis-tops { grid-template-columns: 1fr 1fr; } }
            @media (max-width: 700px) {
                .vis-cards { grid-template-columns: 1fr 1fr; gap: 10px; }
                .vis-card b { font-size: 1.5rem; }
                .vis-tops { grid-template-columns: 1fr; }
                .vis-chart { height: 120px; gap: 1px; }
            }
            /* ===== Map locations: workshop row + pending notice ===== */
            .ws-admin-row td { background: rgba(212,175,55,0.08) !important; }
            .ws-admin-tag { display: inline-block; font-size: 0.7rem; font-weight: 700; color: #111; background: #D4AF37; border-radius: 4px; padding: 1px 8px; margin-bottom: 4px; }
            .tab-count { display: inline-grid; place-items: center; min-width: 20px; height: 20px; margin-inline-start: 6px; padding: 0 6px; border-radius: 99px; background: #f87171; color: #111; font-size: 0.72rem; font-weight: 800; }
            .tab-count[hidden] { display: none; }
            .pin-notice { display: flex; align-items: center; gap: 10px 14px; flex-wrap: wrap; margin: 10px 28px 0; padding: 10px 16px; border: 1px solid rgba(248,113,113,0.5); border-inline-start: 3px solid #f87171; border-radius: 10px; background: #1b1414; font-size: 0.88rem; color: #eee; }
            .pin-notice[hidden] { display: none; }
            .pin-notice-dot { width: 8px; height: 8px; border-radius: 50%; background: #f87171; }
            .pin-notice button { margin-inline-start: auto; background: none; border: 1px solid #f87171; color: #fca5a5; border-radius: 8px; padding: 6px 12px; font: inherit; font-size: 0.8rem; cursor: pointer; }
            @media (max-width: 600px) { .pin-notice { margin: 10px 14px 0; } .pin-notice button { margin-inline-start: 0; } }

            /* ===== Monthly upkeep (الإدامة) / billing ===== */
            .bill-info { position: relative; display: inline-grid; place-items: center; width: 20px; height: 20px; margin-inline-start: 6px; border-radius: 50%; border: 1.5px solid currentColor; background: transparent; color: #D4AF37; font: 800 12px/1 Arial, sans-serif; cursor: help; vertical-align: middle; padding: 0; }
            .invoice .bill-info { color: #8a7340; }
            .bill-info:focus-visible { outline: 2px solid #D4AF37; outline-offset: 2px; }
            .bill-pop { position: absolute; top: calc(100% + 10px); inset-inline-start: -12px; z-index: 50; width: min(360px, 86vw); padding: 14px 16px; background: #fbfaf7; color: #222; border: 1px solid #d8d2c4; border-radius: 10px; box-shadow: 0 18px 44px rgba(0,0,0,0.35); text-align: start; font: 400 0.82rem/1.75 'Segoe UI', Tahoma, Arial, sans-serif; cursor: auto; display: none; }
            .bill-pop strong { display: block; font-size: 0.9rem; color: #111; margin-bottom: 6px; }
            .bill-pop ul { list-style: none; display: grid; gap: 6px; }
            .bill-pop li { padding-inline-start: 12px; border-inline-start: 2px solid #c9b48a; }
            .bill-pop b { color: #6e5222; }
            .bill-info:hover .bill-pop, .bill-info:focus .bill-pop, .bill-info.open .bill-pop { display: block; }
            @media (max-width: 600px) { .bill-pop { position: fixed; inset: auto 12px 12px 12px; top: auto; width: auto; } }
            /* ===== billing layout ===== */
            .ltr { direction: ltr; unicode-bidi: isolate; display: inline-block; }
            .bill-strip { display: flex; align-items: center; gap: 14px 22px; flex-wrap: wrap; margin: 14px 28px 0; padding: 12px 18px; border: 1px solid #2c2c2c; border-inline-start: 3px solid #D4AF37; border-radius: 10px; background: #171717; font-size: 0.88rem; }
            .bill-strip[hidden] { display: none; }
            .bill-strip.s-paid { border-inline-start-color: #4ade80; }
            .bill-strip.s-due { border-inline-start-color: #D4AF37; }
            .bill-strip.s-overdue { border-inline-start-color: #f87171; }
            .bill-strip-main { display: flex; align-items: center; gap: 10px; font-weight: 700; }
            .bill-dot { width: 8px; height: 8px; border-radius: 50%; background: #D4AF37; }
            .s-paid .bill-dot { background: #4ade80; } .s-overdue .bill-dot { background: #f87171; }
            .bill-strip-label { color: #ddd; }
            .bill-strip-status { color: #aaa; font-weight: 600; }
            .bill-strip-meta { color: #9a9a9a; flex: 1 1 260px; }
            .bill-strip-link { background: none; border: 1px solid #3a3a3a; color: #D4AF37; border-radius: 8px; padding: 7px 14px; font: inherit; font-size: 0.82rem; cursor: pointer; }
            .bill-strip-link:hover { border-color: #D4AF37; }

            .bill-layout { display: grid; grid-template-columns: minmax(0, 1.6fr) minmax(260px, 1fr); gap: 20px; align-items: start; }
            .invoice { background: #fbfaf7; color: #1b1b1b; border-radius: 12px; padding: 30px 32px; box-shadow: 0 20px 50px rgba(0,0,0,0.35); }
            .invoice-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; padding-bottom: 20px; border-bottom: 2px solid #1b1b1b; }
            .invoice-kicker { font-size: 0.75rem; letter-spacing: 0.12em; color: #8a7340; font-weight: 700; }
            .invoice-title { font-size: 1.45rem; margin: 4px 0 2px; color: #111; }
            .invoice-sub { color: #6b6b6b; font-size: 0.85rem; }
            .invoice-essential { display: inline-block; margin-top: 8px; padding: 3px 10px; border-inline-start: 3px solid #8a7340; background: #f1ece0; color: #3d3528; font-size: 0.8rem; font-weight: 600; }
            .invoice-status { font-size: 0.78rem; font-weight: 700; padding: 6px 14px; border-radius: 99px; white-space: nowrap; border: 1.5px solid currentColor; }
            .invoice-status.s-paid { color: #15803d; } .invoice-status.s-due { color: #a16207; } .invoice-status.s-overdue { color: #b91c1c; } .invoice-status.s-upcoming { color: #555; }
            .invoice-meta { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; margin: 20px 0 22px; }
            .invoice-meta dt { font-size: 0.72rem; color: #7a7a7a; margin-bottom: 3px; }
            .invoice-meta dd { font-weight: 700; font-size: 0.92rem; }
            .invoice-lines { width: 100%; border-collapse: collapse; background: transparent; border-radius: 0; }
            .invoice-lines td { padding: 12px 0; border-bottom: 1px solid #e3e0d8; color: #222; background: transparent; }
            .invoice-lines .num { text-align: end; direction: ltr; font-variant-numeric: tabular-nums; font-weight: 600; }
            .invoice-lines tfoot td { border-bottom: 0; border-top: 2px solid #1b1b1b; padding-top: 16px; font-size: 1.15rem; font-weight: 800; color: #111; }
            .invoice-lines .credit-row td { color: #15803d; }
            .invoice-note { margin-top: 18px; font-size: 0.8rem; color: #6b6b6b; line-height: 1.8; }

            .bill-side { display: grid; gap: 16px; }
            .bill-card { background: #161616; border: 1px solid #2a2a2a; border-radius: 12px; padding: 18px; }
            .bill-card h3 { font-size: 0.95rem; color: #D4AF37; margin-bottom: 14px; font-weight: 700; }
            .bill-card label { display: block; font-size: 0.8rem; color: #aaa; margin-bottom: 12px; }
            .bill-card label input { display: block; width: 100%; margin-top: 6px; padding: 10px 12px; border-radius: 8px; border: 1px solid #333; background: #0f0f0f; color: #eee; font: inherit; font-size: 0.95rem; }
            .bill-card label input:focus { outline: none; border-color: #D4AF37; }
            .bill-btn { width: 100%; min-height: 44px; border: 0; border-radius: 8px; background: #D4AF37; color: #111; font: inherit; font-weight: 700; cursor: pointer; }
            .bill-btn:disabled { opacity: 0.5; cursor: default; }
            .bill-msg { margin-top: 10px; font-size: 0.82rem; min-height: 1.2em; }
            .bill-msg.ok { color: #4ade80; } .bill-msg.err { color: #f87171; }
            .bill-totals > div { display: flex; justify-content: space-between; padding: 9px 0; border-bottom: 1px solid #242424; font-size: 0.88rem; }
            .bill-totals > div:last-child { border: 0; font-weight: 700; color: #fff; }
            .bill-totals dt { color: #999; }
            .bill-wide { margin-top: 20px; }
            .bill-table td.num { direction: ltr; text-align: end; font-variant-numeric: tabular-nums; }
            .bill-pill { display: inline-block; font-size: 0.72rem; font-weight: 700; padding: 2px 10px; border-radius: 99px; border: 1px solid currentColor; }
            .bill-pill.s-paid { color: #4ade80; } .bill-pill.s-partial { color: #fbbf24; } .bill-pill.s-due { color: #D4AF37; } .bill-pill.s-overdue { color: #f87171; }
            @media (max-width: 1000px) {
                .bill-layout { grid-template-columns: 1fr; }
                .invoice-meta { grid-template-columns: 1fr 1fr; }
            }
            @media (max-width: 600px) {
                .bill-strip { margin: 12px 14px 0; }
                .invoice { padding: 20px 18px; }
                .invoice-head { flex-direction: column; }
                .invoice-title { font-size: 1.2rem; }
                .bill-card label input { font-size: 16px; }
            }
            .admin-tabs { display: flex; gap: 8px; padding: 16px 28px 0; flex-wrap: wrap; }
            .tab-btn {
                padding: 10px 18px; border-radius: 10px 10px 0 0; border: 1px solid rgba(212,175,55,0.25); border-bottom: none;
                background: #161616; color: #bbb; cursor: pointer; font-size: 0.88rem; font-weight: 600; font-family: inherit;
            }
            .tab-btn.active { background: #1e1e1e; color: #D4AF37; }

            .tab-panel { display: none; padding: 24px 28px 60px; background: #1e1e1e; min-height: 60vh; }
            .tab-panel.active { display: block; }

            .stat-banner {
                display: inline-block; background: rgba(212,175,55,0.1); border: 1px solid rgba(212,175,55,0.3);
                border-radius: 10px; padding: 10px 20px; margin-bottom: 18px; font-size: 0.95rem; color: #D4AF37; font-weight: 700;
            }

            table { width: 100%; border-collapse: collapse; background: #161616; border-radius: 10px; overflow: hidden; }
            th, td { padding: 12px 14px; text-align: right; font-size: 0.85rem; border-bottom: 1px solid rgba(255,255,255,0.06); }
            th { background: #111; color: #D4AF37; font-weight: 700; white-space: nowrap; }
            td { color: #ddd; }
            tr:last-child td { border-bottom: none; }
            .table-wrap { overflow-x: auto; }

            .small-btn {
                padding: 6px 12px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.15); background: #222;
                color: #eee; cursor: pointer; font-size: 0.78rem; font-family: inherit; margin: 2px;
            }
            .small-btn.danger { border-color: rgba(255,107,107,0.4); color: #ff9b9b; }
            .small-btn.danger:hover { background: rgba(255,107,107,0.15); }
            .small-btn.primary { border-color: #D4AF37; color: #D4AF37; }
            .small-btn.primary:hover { background: rgba(212,175,55,0.15); }
            .small-btn.success { border-color: #6bff6b; color: #6bff6b; }
            .small-btn.success:hover { background: rgba(107,255,107,0.15); }
            .small-btn.warn { border-color: #ffb86b; color: #ffb86b; }
            .small-btn.warn:hover { background: rgba(255,184,107,0.15); }

            select.status-select {
                background: #111; color: #fff; border: 1px solid rgba(212,175,55,0.3); border-radius: 6px;
                padding: 5px 8px; font-family: inherit; font-size: 0.8rem;
            }

            .content-lang-tabs { display: flex; gap: 8px; margin-bottom: 18px; }
            .lang-tab-btn {
                padding: 8px 20px; border-radius: 50px; border: 1px solid rgba(212,175,55,0.3); background: transparent;
                color: #ccc; cursor: pointer; font-family: inherit; font-size: 0.85rem; font-weight: 700;
            }
            .lang-tab-btn.active { background: #D4AF37; color: #000; }

            .content-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 20px; }
            .field-row { display: flex; flex-direction: column; gap: 5px; background: #161616; border: 1px solid rgba(255,255,255,0.06); border-radius: 10px; padding: 10px 12px; }
            .field-row label { font-size: 0.72rem; color: #D4AF37; font-weight: 700; direction: ltr; text-align: left; }
            .field-row textarea {
                background: #0f0f10; color: #eee; border: 1px solid rgba(255,255,255,0.1); border-radius: 8px;
                padding: 8px 10px; font-family: inherit; font-size: 0.85rem; resize: vertical;
            }
            .field-row textarea:focus { outline: none; border-color: #D4AF37; }

            fieldset { border: 1px solid rgba(212,175,55,0.25); border-radius: 10px; padding: 16px; margin-bottom: 20px; }
            legend { color: #D4AF37; font-size: 0.85rem; font-weight: 700; padding: 0 8px; }
            fieldset input[type="text"] {
                width: 100%; padding: 9px 12px; margin-bottom: 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.12);
                background: #0f0f10; color: #eee; font-family: inherit; font-size: 0.85rem;
            }
            fieldset input[type="text"]:focus { outline: none; border-color: #D4AF37; }

            .pin-form-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 12px; }
            .pin-form-grid input {
                width: 100%; padding: 9px 12px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.12);
                background: #0f0f10; color: #eee; font-family: inherit; font-size: 0.85rem;
            }
            .pin-form-grid input:focus { outline: none; border-color: #D4AF37; }
            .pin-form-note { font-size: 0.75rem; color: #999; margin-bottom: 10px; }

            .archival-form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px; }
            .archival-form-grid input, .archival-form-grid textarea {
                width: 100%; padding: 9px 12px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.12);
                background: #0f0f10; color: #eee; font-family: inherit; font-size: 0.85rem;
            }
            .archival-form-grid textarea { resize: vertical; min-height: 60px; }
            .archival-form-grid input:focus, .archival-form-grid textarea:focus { outline: none; border-color: #D4AF37; }

            .upload-box { display: flex; align-items: center; gap: 10px; margin-bottom: 20px; flex-wrap: wrap; }
            .upload-box input[type="file"] { color: #ccc; font-size: 0.8rem; }
            #uploadResult { font-size: 0.78rem; color: #9fd39f; word-break: break-all; }

            .save-btn {
                padding: 12px 30px; border-radius: 10px; border: none; background: #D4AF37; color: #000;
                font-weight: 700; cursor: pointer; font-family: inherit; font-size: 0.9rem;
            }
            .save-btn:hover { background: #C0951C; }

            .editing-banner {
                display: none; background: rgba(212,175,55,0.15); border: 1px solid #D4AF37; border-radius: 8px;
                padding: 8px 14px; margin-bottom: 12px; font-size: 0.8rem; color: #D4AF37; align-items: center;
                justify-content: space-between; gap: 10px;
            }
            .editing-banner.active { display: flex; }
            .editing-banner button {
                background: transparent; border: 1px solid rgba(212,175,55,0.4); color: #D4AF37; border-radius: 6px;
                padding: 4px 10px; cursor: pointer; font-family: inherit; font-size: 0.75rem;
            }

            /* ===== ADMIN SPLIT VIEW: MAP + TABLES SIDE-BY-SIDE ===== */
            .admin-split-view {
                display: grid;
                grid-template-columns: 1fr 1fr;
                gap: 24px;
                margin-top: 20px;
                align-items: start;
            }

            /* Left column: Map card */
            .admin-map-column {
                position: sticky;
                top: 20px;
            }
            .admin-map-card {
                background: #161616;
                border: 1px solid rgba(212,175,55,0.3);
                border-radius: 16px;
                padding: 16px;
                box-shadow: 0 8px 32px rgba(212,175,55,0.06);
                overflow: hidden;
            }
            .admin-map-card .admin-map-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: 12px;
            }
            .admin-map-card .admin-map-header h3 {
                color: #D4AF37;
                font-size: 0.95rem;
                display: flex;
                align-items: center;
                gap: 8px;
            }
            .admin-map-card .admin-map-header .map-hint {
                font-size: 0.75rem;
                color: #999;
            }

            /* RIGHT COLUMN: Tables */
            .admin-tables-column {
                display: flex;
                flex-direction: column;
                gap: 20px;
            }

            /* Fix Map Container: 480px height for better vertical scanning */
            .admin-map-container {
                height: 480px;
                width: 100%;
                border-radius: 12px;
                overflow: hidden;
                position: relative;
                border: 1px solid rgba(212,175,55,0.2);
                background: #0f0f10;
            }
            .admin-map-container #adminMap {
                height: 480px;
                width: 100%;
                border-radius: 12px;
            }

            .leaflet-control-attribution {
                display: none !important;
            }

            .leaflet-control-layers {
                background: #1a1a1a !important;
                border: 1px solid rgba(212,175,55,0.3) !important;
                border-radius: 10px !important;
                box-shadow: 0 4px 20px rgba(0,0,0,0.4) !important;
                font-family: 'Segoe UI', 'Helvetica Neue', Arial, sans-serif !important;
            }
            .leaflet-control-layers label {
                color: #eee !important;
                font-size: 0.85rem !important;
                padding: 4px 8px !important;
                cursor: pointer !important;
            }
            .leaflet-control-layers label:hover {
                background: rgba(212,175,55,0.1) !important;
            }
            .leaflet-control-layers label input[type="radio"] {
                accent-color: #D4AF37 !important;
            }

            .admin-map-instruction {
                position: absolute;
                top: 16px;
                left: 16px;
                z-index: 1000;
                background: rgba(0,0,0,0.75);
                color: #D4AF37;
                padding: 8px 16px;
                border-radius: 50px;
                font-size: 0.78rem;
                font-weight: 600;
                backdrop-filter: blur(8px);
                border: 1px solid rgba(212,175,55,0.3);
            }

            /* Admin section boxes for tables */
            .admin-section-box {
                background: #161616;
                border: 1px solid rgba(212,175,55,0.2);
                border-radius: 12px;
                padding: 18px;
            }
            .admin-section-box h3 {
                color: #D4AF37;
                font-size: 0.95rem;
                margin-bottom: 12px;
                display: flex;
                align-items: center;
                gap: 8px;
            }
            .admin-section-box h3 .badge {
                background: rgba(212,175,55,0.15);
                color: #D4AF37;
                font-size: 0.7rem;
                padding: 2px 10px;
                border-radius: 50px;
                font-weight: 700;
            }
            .admin-section-box h3 .badge.pending {
                background: rgba(255,184,107,0.15);
                color: #ffb86b;
            }

            .admin-two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }

            /* ---- Accordion / Dropdown Panels for Admin Tables ---- */
            .accordion-panel {
                background: #161616; border: 1px solid rgba(212,175,55,0.2); border-radius: 12px;
                margin-bottom: 16px; overflow: hidden;
            }
            .accordion-header {
                display: flex; justify-content: space-between; align-items: center;
                padding: 16px 20px; cursor: pointer; user-select: none;
                background: #111; transition: background 0.2s ease;
            }
            .accordion-header:hover { background: #1a1a1a; }
            .accordion-header h3 {
                color: #D4AF37; font-size: 0.95rem; display: flex; align-items: center;
                gap: 8px; margin: 0;
            }
            .accordion-header h3 .badge {
                background: rgba(212,175,55,0.15); color: #D4AF37; font-size: 0.7rem;
                padding: 2px 10px; border-radius: 50px; font-weight: 700;
            }
            .accordion-header h3 .badge.pending {
                background: rgba(255,184,107,0.15); color: #ffb86b;
            }
            .accordion-arrow {
                color: #D4AF37; font-size: 0.9rem; transition: transform 0.3s ease;
                width: 24px; height: 24px; display: flex; align-items: center; justify-content: center;
            }
            .accordion-panel.collapsed .accordion-arrow { transform: rotate(-90deg); }
            .accordion-body {
                max-height: 600px; overflow: hidden; transition: max-height 0.4s ease, padding 0.3s ease;
                padding: 16px 20px 20px;
            }
            .accordion-panel.collapsed .accordion-body {
                max-height: 0; padding: 0 20px;
            }

            /* ---- Modal for Direct Pin Drop ---- */
            .admin-modal-overlay {
                position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 9999;
                background: rgba(0,0,0,0.7); backdrop-filter: blur(4px);
                display: none; align-items: center; justify-content: center;
                animation: fadeIn 0.25s ease;
            }
            .admin-modal-overlay.active { display: flex; }
            .admin-modal-box {
                background: #1a1a1a; border-radius: 20px; max-width: 700px; width: 95%;
                max-height: 90vh; overflow-y: auto; padding: 30px;
                border: 1px solid rgba(212,175,55,0.3); box-shadow: 0 30px 80px rgba(0,0,0,0.6);
                position: relative;
            }
            .admin-modal-box .modal-close {
                position: absolute; top: 14px; right: 18px; background: transparent; border: none;
                font-size: 1.8rem; cursor: pointer; color: #999; transition: 0.3s; line-height: 1;
            }
            .admin-modal-box .modal-close:hover { color: #fff; transform: rotate(90deg); }
            .admin-modal-box h2 { color: #D4AF37; font-size: 1.2rem; margin-bottom: 18px; }
            .admin-modal-box .modal-map-wrap {
                position: relative; width: 100%; padding-top: 60%; border-radius: 12px;
                overflow: hidden; border: 1px solid rgba(212,175,55,0.3); margin-bottom: 20px;
            }
            .admin-modal-box .modal-map-wrap #modalAdminMap {
                position: absolute; top: 0; left: 0; width: 100%; height: 100%;
                border-radius: 12px;
            }
            .admin-modal-box .coords-display {
                background: #0f0f10; border: 1px solid rgba(212,175,55,0.2); border-radius: 8px;
                padding: 10px 14px; margin-bottom: 16px; font-size: 0.82rem; color: #D4AF37;
                direction: ltr; text-align: center;
            }
            .admin-modal-box .form-group { margin-bottom: 14px; }
            .admin-modal-box .form-group label {
                display: block; font-size: 0.8rem; font-weight: 700; color: #D4AF37;
                margin-bottom: 4px;
            }
            .admin-modal-box .form-group input {
                width: 100%; padding: 10px 14px; border-radius: 10px; border: 1px solid rgba(255,255,255,0.12);
                background: #0f0f10; color: #eee; font-size: 0.95rem; font-family: inherit; transition: 0.3s;
            }
            .admin-modal-box .form-group input:focus { outline: none; border-color: #D4AF37; box-shadow: 0 0 0 3px rgba(212,175,55,0.2); }

            /* Modern modal action buttons (gradient, hover polish) */
            .admin-modal-box .modal-actions {
                display: flex; gap: 12px; margin-top: 10px; flex-wrap: wrap;
            }
            .admin-modal-box .modal-actions .btn-confirm {
                flex: 1; display: inline-flex; align-items: center; justify-content: center;
                gap: 8px; padding: 14px 28px; border: none; border-radius: 12px;
                background: linear-gradient(135deg, #D4AF37 0%, #C0951C 100%);
                color: #000; font-weight: 800; font-size: 0.95rem; cursor: pointer;
                font-family: inherit; transition: all 0.3s cubic-bezier(0.4,0,0.2,1);
                box-shadow: 0 6px 24px rgba(212,175,55,0.35);
            }
            .admin-modal-box .modal-actions .btn-confirm:hover {
                background: linear-gradient(135deg, #E6C44A 0%, #D4AF37 100%);
                transform: translateY(-2px);
                box-shadow: 0 10px 36px rgba(212,175,55,0.55);
            }
            .admin-modal-box .modal-actions .btn-cancel {
                flex: 1; display: inline-flex; align-items: center; justify-content: center;
                gap: 8px; padding: 14px 28px; border: 2px solid rgba(212,175,55,0.45);
                border-radius: 12px; background: transparent; color: #D4AF37;
                font-weight: 700; font-size: 0.95rem; cursor: pointer;
                font-family: inherit; transition: all 0.3s cubic-bezier(0.4,0,0.2,1);
            }
            .admin-modal-box .modal-actions .btn-cancel:hover {
                background: rgba(212,175,55,0.12);
                border-color: #D4AF37; color: #E6C44A;
                transform: translateY(-2px);
                box-shadow: 0 8px 24px rgba(212,175,55,0.25);
            }

            @media (max-width: 1100px) {
                .admin-split-view { grid-template-columns: 1fr; }
                .admin-map-column { position: static; }
            }
            @media (max-width: 900px) {
                .content-fields { grid-template-columns: 1fr; }
                .pin-form-grid { grid-template-columns: 1fr 1fr; }
                .archival-form-grid { grid-template-columns: 1fr; }
                .admin-header, .admin-tabs, .tab-panel { padding-left: 14px; padding-right: 14px; }
                .admin-two-col { grid-template-columns: 1fr; }
                .accordion-body { max-height: 400px; }
            }
            @media (max-width: 600px) {
                .pin-form-grid { grid-template-columns: 1fr; }
                .admin-map-container { height: 350px; }
                .admin-map-container #adminMap { height: 350px; }
                .admin-modal-box .modal-map-wrap { padding-top: 80%; }
                .admin-modal-box { padding: 20px; }
                /* phone: header scrolls away, tabs become one swipeable row */
                .admin-header { position: static; padding-top: 14px; padding-bottom: 14px; }
                .admin-header h1 { font-size: 1.05rem; }
                .admin-header .actions { flex-wrap: nowrap; overflow-x: auto; width: 100%; padding-bottom: 4px; scrollbar-width: none; }
                .admin-header .actions > * { flex: 0 0 auto; }
                .admin-tabs { flex-wrap: nowrap; overflow-x: auto; scrollbar-width: none; position: sticky; top: 0; z-index: 9; background: #0f0f10; padding-top: 10px; padding-bottom: 0; }
                .admin-tabs .tab-btn { flex: 0 0 auto; }
                .tab-panel { padding-top: 18px; }
            }
            @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
            @keyframes pulse { 0% { opacity: 1; transform: scale(1); } 50% { opacity: 0.5; transform: scale(1.3); } 100% { opacity: 1; transform: scale(1); } }
            """;

            string body = """
            <body>
                <div id="loginGate">
                    <div class="login-box">
                        <h2>تسجيل الدخول — لوحة التحكم</h2>
                        <input type="email" id="loginEmailInput" placeholder="البريد الإلكتروني" />
                        <input type="password" id="loginPasswordInput" placeholder="كلمة المرور" />
                        <button onclick="adminLogin()">دخول</button>
                        <p class="error-text" id="loginError"></p>
                    </div>
                </div>

                <div id="dashboard" class="hidden">
                    <header class="admin-header">
                        <h1>لوحة تحكم ورشة الفرات الأوسط</h1>
                        <div class="actions">
                            <!-- ===== بيانات الحساب الحالي (دائماً ظاهرة) ===== -->
                            <div id="headerAccountInfo" style="display:none;align-items:center;gap:8px;background:rgba(212,175,55,0.08);border:1px solid rgba(212,175,55,0.25);border-radius:50px;padding:6px 14px;">
                                
                                <span id="headerAccountLabel" style="color:#D4AF37;font-weight:700;font-size:0.82rem;"></span>
                                <span style="color:#4ade80;font-size:0.75rem;"><span style="display:inline-block;width:7px;height:7px;border-radius:50%;background:#4ade80;margin-inline-end:5px;vertical-align:middle;"></span>متصل</span>
                                <span id="headerRoleBadge" style="font-size:0.7rem;padding:2px 8px;border-radius:50px;font-weight:700;"></span>
                            </div>

                            <a href="/" target="_blank">عرض الموقع ↗</a>
                            <a href="/explorer" target="_blank">المستكشف ↗</a>
                            <a href="/guide" target="_blank">الدليل الشامل ↗</a>
                            <a href="/industrial-services" target="_blank">الخدمات الصناعية ↗</a>
                            <button onclick="adminLogout()">تسجيل الخروج</button>
                        </div>
                    </header>

                    <!-- ===== SUBSCRIPTION STATUS STRIP ===== -->
                    <div class="bill-strip" id="billStrip" hidden>
                        <div class="bill-strip-main">
                            <span class="bill-dot" id="billStripDot"></span>
                            <span class="bill-strip-label">الإدامة الشهرية للموقع</span><button type="button" class="bill-info" aria-label="لماذا هذه الفاتورة؟" onclick="toggleBillInfo(event, this)">!</button>
                            <span class="bill-strip-status" id="billStripStatus"></span>
                        </div>
                        <div class="bill-strip-meta" id="billStripMeta"></div>
                        <button type="button" class="bill-strip-link" onclick="switchAdminTab('billing')">عرض الفاتورة</button>
                    </div>

                    <div class="pin-notice" id="pinPendingNotice" hidden>
                        <span class="pin-notice-dot"></span>
                        <span id="pinPendingText"></span>
                        <button type="button" onclick="switchAdminTab('industrial'); setTimeout(function(){ var el=document.getElementById('pendingPinsBody'); if (el) el.scrollIntoView({behavior:'smooth', block:'center'}); }, 200);">مراجعة الطلبات</button>
                    </div>

                    <nav class="admin-tabs">
                        <button class="tab-btn active" data-tab="accounts" onclick="switchAdminTab('accounts')">المستخدمين والحسابات</button>
                        <button class="tab-btn" data-tab="requests" onclick="switchAdminTab('requests')">طلبات الخدمة</button>
                        <button class="tab-btn" data-tab="content" onclick="switchAdminTab('content')">محتوى الصفحة الرئيسية</button>
                        <button class="tab-btn" data-tab="industrial" onclick="switchAdminTab('industrial')">الخدمات الصناعية والخريطة</button>
                        <button class="tab-btn" data-tab="gallery" onclick="switchAdminTab('gallery')">معرض الصور والفيديو</button>
                        <button class="tab-btn" data-tab="album" onclick="switchAdminTab('album')">الأرشيف والصور التاريخية</button>
                        <button class="tab-btn" data-tab="explorer" onclick="switchAdminTab('explorer')">بطاقات المستكشف</button>
                        <button class="tab-btn" data-tab="guide" onclick="switchAdminTab('guide')">محتوى الدليل</button>
                        <button class="tab-btn" data-tab="visitors" onclick="switchAdminTab('visitors')">الزوار</button>
                        <button class="tab-btn" data-tab="billing" onclick="switchAdminTab('billing')">الفوترة والإدامة</button>
                    </nav>

                    <!-- ===== USERS & ACCOUNTS TAB ===== -->
                    <section id="tab-accounts" class="tab-panel active">

                        <!-- Account status moved to header -->

                        <!-- Create Account Card — ADMIN ONLY -->
                        <div id="createAccountCard" class="admin-section-box" style="margin-bottom:20px;display:none;">
                            <h3>إنشاء حساب جديد <span class="badge">Admin فقط</span></h3>
                            <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:12px;">
                                <div class="form-group" style="margin:0;">
                                    <label style="display:block;font-size:0.8rem;color:#D4AF37;margin-bottom:4px;">الاسم الكامل</label>
                                    <input type="text" id="newAccountFullName" placeholder="مثال: أحمد محمد" style="width:100%;padding:10px 14px;border-radius:10px;border:1px solid rgba(255,255,255,0.12);background:#0f0f10;color:#eee;font-family:inherit;" />
                                </div>
                                <div class="form-group" style="margin:0;">
                                    <label style="display:block;font-size:0.8rem;color:#D4AF37;margin-bottom:4px;">البريد الإلكتروني</label>
                                    <input type="email" id="newAccountEmail" placeholder="name@example.com" style="width:100%;padding:10px 14px;border-radius:10px;border:1px solid rgba(255,255,255,0.12);background:#0f0f10;color:#eee;font-family:inherit;" />
                                </div>
                                <div class="form-group" style="margin:0;">
                                    <label style="display:block;font-size:0.8rem;color:#D4AF37;margin-bottom:4px;">كلمة المرور</label>
                                    <input type="password" id="newAccountPassword" placeholder="6 أحرف على الأقل" style="width:100%;padding:10px 14px;border-radius:10px;border:1px solid rgba(255,255,255,0.12);background:#0f0f10;color:#eee;font-family:inherit;" />
                                </div>
                                <div class="form-group" style="margin:0;">
                                    <label style="display:block;font-size:0.8rem;color:#D4AF37;margin-bottom:4px;">الدور</label>
                                    <select id="newAccountRole" style="width:100%;padding:10px 14px;border-radius:10px;border:1px solid rgba(255,255,255,0.12);background:#0f0f10;color:#eee;font-family:inherit;">
                                        <option value="Manager" selected>Manager (مدير)</option>
                                        <option value="Admin">Admin (أدمن)</option>
                                    </select>
                                </div>
                            </div>
                            <div style="margin-top:14px;">
                                <button class="small-btn primary" onclick="createNewAccount()" style="padding:10px 24px;">إنشاء الحساب</button>
                                <span id="createAccountMsg" style="margin-right:12px;font-size:0.85rem;"></span>
                            </div>
                        </div>

                        <!-- Accounts Table -->
                        <div class="admin-section-box">
                            <h3>الحسابات المسجلة <span class="badge" id="accountsCount">0</span></h3>
                            <div class="table-wrap">
                                <table>
                                    <thead>
                                        <tr>
                                            <th>الاسم الكامل</th>
                                            <th>البريد الإلكتروني</th>
                                            <th>الدور</th>
                                            <th>الحالة</th>
                                            <th>تاريخ الإنشاء</th>
                                            <th>الإجراءات</th>
                                        </tr>
                                    </thead>
                                    <tbody id="accountsBody"></tbody>
                                </table>
                            </div>
                        </div>
                    </section>

                    <section id="tab-requests" class="tab-panel">
                        <div class="table-wrap">
                            <table>
                                <thead>
                                    <tr>
                                        <th>الاسم الكامل</th><th>الهاتف</th><th>نوع الخدمة</th><th>التفاصيل</th>
                                        <th>تاريخ الإرسال</th><th>الحالة</th><th></th>
                                    </tr>
                                </thead>
                                <tbody id="requestsBody"></tbody>
                            </table>
                        </div>
                    </section>

                    <section id="tab-content" class="tab-panel">
                        <div class="content-lang-tabs">
                            <button class="lang-tab-btn active" data-lang="ar" onclick="switchContentLang('ar')">العربية</button>
                            <button class="lang-tab-btn" data-lang="en" onclick="switchContentLang('en')">English</button>
                            <button class="lang-tab-btn" data-lang="ku" onclick="switchContentLang('ku')">کوردی</button>
                        </div>
                        <div id="contentFields-ar" class="content-fields"></div>
                        <div id="contentFields-en" class="content-fields hidden"></div>
                        <div id="contentFields-ku" class="content-fields hidden"></div>

                        <fieldset>
                            <legend>صور شريط الواجهة (Hero Slider)</legend>
                            <input type="text" id="heroImage0" placeholder="رابط صورة 1" />
                            <input type="text" id="heroImage1" placeholder="رابط صورة 2" />
                            <input type="text" id="heroImage2" placeholder="رابط صورة 3" />
                            <input type="text" id="heroImage3" placeholder="رابط صورة 4" />
                        </fieldset>

                        <div class="upload-box">
                            <input type="file" id="imageUploadInput" accept="image/*" />
                            <button class="small-btn primary" onclick="uploadImage()">رفع صورة</button>
                            <span id="uploadResult"></span>
                        </div>

                        <button class="save-btn" onclick="saveContent()">حفظ كل تغييرات الصفحة الرئيسية</button>
                    </section>

                    <section id="tab-industrial" class="tab-panel">
                        <div class="content-lang-tabs">
                            <button class="lang-tab-btn active" data-lang="ar" onclick="switchIndLang('ar')">العربية</button>
                            <button class="lang-tab-btn" data-lang="en" onclick="switchIndLang('en')">English</button>
                            <button class="lang-tab-btn" data-lang="ku" onclick="switchIndLang('ku')">کوردی</button>
                        </div>
                        <div id="indFields-ar" class="content-fields"></div>
                        <div id="indFields-en" class="content-fields hidden"></div>
                        <div id="indFields-ku" class="content-fields hidden"></div>

                        <fieldset>
                            <legend>رابط خريطة جوجل الافتراضي (Google Maps Embed URL)</legend>
                            <input type="text" id="mapEmbedUrlInput" placeholder="https://www.google.com/maps?q=...&output=embed" />
                        </fieldset>

                        <button class="save-btn" onclick="saveIndustrialContent()">حفظ تغييرات الخدمات الصناعية</button>

                        <!-- ===== ADMIN INTERACTIVE MAP + TABLES — SIDE-BY-SIDE SPLIT VIEW WITH ACCORDION ===== -->
                        <fieldset style="margin-top: 26px;">
                            <legend>الخريطة التفاعلية — إدارة المواقع والأقسام</legend>

                            <div class="admin-split-view">
                                <!-- LEFT COLUMN: Map Card (480px height for better vertical scanning) -->
                                <div class="admin-map-column">
                                    <div class="admin-map-card">
                                        <div class="admin-map-header">
                                            <h3>الخريطة التفاعلية</h3>
                                            <span class="map-hint">انقر على الخريطة لإضافة موقع</span>
                                        </div>
                                        <div class="admin-map-container">
                                            <div id="adminMap"></div>
                                            <div class="admin-map-instruction">انقر على الخريطة لإضافة موقع جديد مباشرة (موافَق عليه تلقائياً)</div>
                                        </div>
                                    </div>
                                </div>

                                <!-- RIGHT COLUMN: Tables as Accordion Panels -->
                                <div class="admin-tables-column">
                                    <!-- Approved Locations Accordion (moved to top) -->
                                    <div class="accordion-panel collapsed" id="approvedAccordion">
                                        <div class="accordion-header" onclick="toggleAccordion('approvedAccordion')">
                                            <h3>الأقسام والمواقع المعتمدة <span class="badge" id="approvedCount">0</span></h3>
                                            <span class="accordion-arrow">▼</span>
                                        </div>
                                        <div class="accordion-body">
                                            <div class="table-wrap">
                                                <table>
                                                    <thead>
                                                        <tr>
                                                            <th>الاسم (عربي)</th><th>الاسم (إنجليزي)</th><th>العنوان (عربي)</th>
                                                            <th>الهاتف</th><th>خط العرض</th><th>خط الطول</th><th>الإجراءات</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody id="mapPinsBody"></tbody>
                                                </table>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- Pending Approval Accordion (now below approved) -->
                                    <div class="accordion-panel collapsed" id="pendingAccordion">
                                        <div class="accordion-header" onclick="toggleAccordion('pendingAccordion')">
                                            <h3>الطلبات المعلقة <span class="badge pending" id="pendingCount">0</span></h3>
                                            <span class="accordion-arrow">▼</span>
                                        </div>
                                        <div class="accordion-body">
                                            <div class="table-wrap">
                                                <table>
                                                    <thead>
                                                        <tr>
                                                            <th>الاسم</th><th>العنوان</th><th>الهاتف</th>
                                                            <th>خط العرض</th><th>خط الطول</th><th>الإجراءات</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody id="pendingPinsBody"></tbody>
                                                </table>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- Admin Pin Form (manual / edit) -->
                            <div class="editing-banner" id="pinEditingBanner" style="margin-top: 20px;">
                                <span id="pinEditingLabel">جاري تعديل قسم...</span>
                                <button onclick="cancelPinEdit()">إلغاء التعديل</button>
                            </div>

                            <div style="margin-top: 16px;">
                                <h4 style="color: #D4AF37; font-size: 0.85rem; margin-bottom: 10px;">إضافة / تعديل قسم يدوياً</h4>
                                <div class="pin-form-grid">
                                    <input type="text" id="pinNameAr" placeholder="الاسم بالعربية" />
                                    <input type="text" id="pinNameEn" placeholder="Name in English" />
                                    <input type="text" id="pinNameKu" placeholder="ناو بە کوردی" />
                                    <input type="text" id="pinAddressAr" placeholder="العنوان (عربي)" />
                                    <input type="text" id="pinAddressEn" placeholder="Address (English)" />
                                    <input type="text" id="pinAddressKu" placeholder="ناونیشان (کوردی)" />
                                    <input type="text" id="pinPhone" placeholder="رقم الهاتف" />
                                    <input type="text" id="pinLat" placeholder="خط العرض (Latitude)" />
                                    <input type="text" id="pinLng" placeholder="خط الطول (Longitude)" />
                                </div>
                                <div class="pin-form-note">اختياري: رابط خريطة مخصص لهذا القسم (Embed URL) — إن تُرك فارغاً سيُستخدم الإحداثيات أعلاه</div>
                                <input type="text" id="pinMapEmbedUrl" placeholder="https://www.google.com/maps?q=...&output=embed (اختياري)" style="width:100%; padding:9px 12px; margin-bottom:12px; border-radius:8px; border:1px solid rgba(255,255,255,0.12); background:#0f0f10; color:#eee; font-family:inherit; font-size:0.85rem;" />

                                <button class="small-btn primary" id="pinSubmitBtn" onclick="submitMapPin()">إضافة قسم جديد</button>
                            </div>
                        </fieldset>
                    </section>

                    <!-- ===== ARCHIVAL ALBUM TAB ===== -->
                    <section id="tab-album" class="tab-panel">
                        <fieldset>
                            <legend>إدارة الألبوم الأرشيفي (الصور التاريخية)</legend>
                            <p style="color: #999; font-size: 0.82rem; margin-bottom: 16px;">
                                أضف الصور التاريخية مع السنة والوصف. تظهر هذه الصور في قسم "نبذة عن مؤسستنا" في الصفحة الرئيسية.
                            </p>

                            <div class="editing-banner" id="albumEditingBanner">
                                <span id="albumEditingLabel">جاري تعديل عنصر...</span>
                                <button onclick="cancelAlbumEdit()">إلغاء التعديل</button>
                            </div>

                            <div class="archival-form-grid">
                                <input type="text" id="albumYear" placeholder="السنة (مثال: 2026)" />
                                <input type="text" id="albumImageUrl" placeholder="رابط الصورة" />
                                <textarea id="albumCaptionAr" placeholder="الوصف بالعربية"></textarea>
                                <textarea id="albumCaptionEn" placeholder="Caption in English"></textarea>
                                <textarea id="albumCaptionKu" placeholder="ناونیشان بە کوردی"></textarea>
                            </div>
                            <div style="margin-bottom: 12px;">
                                <input type="file" id="albumImageUploadInput" accept="image/*" style="color: #ccc; font-size: 0.8rem;" />
                                <button class="small-btn primary" onclick="uploadAlbumImage()">رفع صورة للأرشيف</button>
                                <span id="albumUploadResult" style="font-size: 0.78rem; color: #9fd39f; word-break: break-all;"></span>
                            </div>
                            <button class="small-btn primary" id="albumSubmitBtn" onclick="submitAlbumItem()">إضافة عنصر جديد</button>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>العناصر الحالية</legend>
                            <div class="table-wrap">
                                <table>
                                    <thead>
                                        <tr>
                                            <th>السنة</th><th>الصورة</th><th>الوصف (عربي)</th>
                                            <th>الوصف (إنجليزي)</th><th>الوصف (كوردي)</th><th>الإجراءات</th>
                                        </tr>
                                    </thead>
                                    <tbody id="albumItemsBody"></tbody>
                                </table>
                            </div>
                        </fieldset>
                    </section>

                    <!-- ===== GALLERY TAB ===== -->
                    <section id="tab-gallery" class="tab-panel">
                        <style>
                            .gal-meter { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; margin-bottom: 14px; }
                            .gal-meter strong { font-size: 1.3rem; color: #D4AF37; direction: ltr; }
                            .gal-meter-bar { flex: 1 1 200px; height: 8px; background: #2a2a2a; border-radius: 99px; overflow: hidden; }
                            .gal-meter-bar span { display: block; height: 100%; background: #D4AF37; width: 0; transition: width .3s; }
                            .gal-meter.full strong { color: #ff9b9b; }
                            .gal-meter.full .gal-meter-bar span { background: #ff6b6b; }
                            .gal-drop { display: block; border: 2px dashed rgba(212,175,55,0.4); border-radius: 10px; padding: 22px 16px; text-align: center; color: #bbb; cursor: pointer; margin-bottom: 12px; }
                            .gal-drop.drag { background: rgba(212,175,55,0.08); border-color: #D4AF37; }
                            .gal-drop input { display: block; margin: 10px auto 0; color: #ccc; max-width: 100%; }
                            .gal-drop small { display: block; color: #888; margin-top: 6px; }
                            .gal-caps { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 12px; }
                            .gal-caps input, .gal-card input { width: 100%; padding: 9px 11px; border-radius: 8px; border: 1px solid #333; background: #111; color: #eee; font: inherit; font-size: 0.85rem; }
                            .gal-caps input:focus, .gal-card input:focus { outline: none; border-color: #D4AF37; }
                            .gal-progress { margin-top: 10px; font-size: 0.85rem; }
                            .gal-progress div { padding: 4px 0; border-bottom: 1px solid #222; display: flex; justify-content: space-between; gap: 10px; }
                            .gal-progress .ok { color: #9fd39f; } .gal-progress .err { color: #ff9b9b; } .gal-progress .run { color: #D4AF37; }
                            .gal-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 14px; }
                            .gal-card { background: #161616; border: 1px solid #2a2a2a; border-radius: 10px; overflow: hidden; display: flex; flex-direction: column; }
                            .gal-thumb { position: relative; aspect-ratio: 4/3; background: #000; }
                            .gal-thumb img, .gal-thumb video { width: 100%; height: 100%; object-fit: cover; display: block; }
                            .gal-badge { position: absolute; top: 8px; inset-inline-start: 8px; background: rgba(0,0,0,.7); color: #D4AF37; font-size: .72rem; padding: 2px 8px; border-radius: 99px; }
                            .gal-pos { position: absolute; top: 8px; inset-inline-end: 8px; background: rgba(0,0,0,.7); color: #eee; font-size: .72rem; padding: 2px 8px; border-radius: 99px; direction: ltr; }
                            .gal-body { padding: 10px; display: grid; gap: 6px; }
                            .gal-actions { display: flex; flex-wrap: wrap; gap: 6px; padding: 0 10px 10px; }
                            .gal-actions .small-btn { flex: 1 1 auto; min-height: 38px; }
                            @media (max-width: 700px) {
                                .gal-caps { grid-template-columns: 1fr; }
                                .gal-grid { grid-template-columns: 1fr 1fr; gap: 10px; }
                                .gal-body input { font-size: 16px; }
                            }
                            @media (max-width: 420px) { .gal-grid { grid-template-columns: 1fr; } }
                        </style>
                        <fieldset>
                            <legend>معرض الصور والفيديو</legend>
                            <div class="gal-meter" id="galMeter">
                                <span>المستخدم:</span> <strong id="galCount">0 / 60</strong>
                                <div class="gal-meter-bar"><span id="galMeterFill"></span></div>
                                <a href="/gallery" target="_blank" class="small-btn primary" style="text-decoration:none;">عرض صفحة المعرض ↗</a>
                            </div>
                            <label class="gal-drop" id="galDrop">
                                اختر صوراً أو مقاطع فيديو (يمكن اختيار عدة ملفات) أو اسحبها إلى هنا
                                <input type="file" id="galFiles" multiple accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/quicktime,video/webm,.mov,.m4v" />
                                <small id="galLimits">الصور حتى 12MB — الفيديو حتى 60MB (MP4 / MOV / WEBM) — الحد الأقصى 60 ملفاً</small>
                            </label>
                            <div class="gal-caps">
                                <input type="text" id="galCapAr" placeholder="الوصف بالعربية (اختياري)" maxlength="300" />
                                <input type="text" id="galCapEn" placeholder="Caption in English (optional)" maxlength="300" />
                                <input type="text" id="galCapKu" placeholder="ناونیشان بە کوردی (ئارەزوومەندانە)" maxlength="300" />
                            </div>
                            <button class="small-btn primary" id="galUploadBtn" onclick="uploadGalleryFiles()">رفع إلى المعرض</button>
                            <div class="gal-progress" id="galProgress"></div>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>محتوى المعرض (الترتيب هنا هو ترتيب العرض في الموقع)</legend>
                            <div class="gal-grid" id="galGrid"></div>
                        </fieldset>
                    </section>

                    <!-- ===== VISITORS TAB ===== -->
                    <section id="tab-visitors" class="tab-panel">
                        <div class="vis-head">
                            <h2>زوار الموقع</h2>
                            <div class="vis-range" role="group" aria-label="الفترة">
                                <button type="button" data-days="7" onclick="loadVisitors(7)">7 أيام</button>
                                <button type="button" data-days="30" class="active" onclick="loadVisitors(30)">30 يوماً</button>
                                <button type="button" data-days="90" onclick="loadVisitors(90)">90 يوماً</button>
                            </div>
                        </div>
                        <div class="vis-cards">
                            <div class="vis-card"><span>اليوم</span><b id="visToday">-</b><small id="visTodayU"></small></div>
                            <div class="vis-card"><span>آخر 7 أيام</span><b id="visWeek">-</b><small id="visWeekU"></small></div>
                            <div class="vis-card"><span id="visPeriodLabel">آخر 30 يوماً</span><b id="visPeriod">-</b><small id="visPeriodU"></small></div>
                            <div class="vis-card"><span>زوار عائدون</span><b id="visReturning">-</b><small id="visConsent"></small></div>
                        </div>
                        <div class="bill-card bill-wide">
                            <h3>الزيارات اليومية</h3>
                            <div class="vis-chart" id="visChart"></div>
                        </div>
                        <div class="vis-tops">
                            <div class="bill-card"><h3>أكثر الصفحات زيارة</h3><ul class="vis-list" id="visPages"></ul></div>
                            <div class="bill-card"><h3>مصدر الزيارة</h3><ul class="vis-list" id="visRefs"></ul></div>
                            <div class="bill-card"><h3>الأجهزة</h3><ul class="vis-list" id="visDevices"></ul></div>
                            <div class="bill-card"><h3>المتصفح ونظام التشغيل</h3><ul class="vis-list" id="visBrowsers"></ul></div>
                        </div>
                        <div class="bill-card bill-wide">
                            <h3>الزوار المعروفون (وافقوا على الكوكيز)</h3>
                            <div class="table-wrap">
                                <table class="bill-table">
                                    <thead><tr><th>الزائر</th><th>أول زيارة</th><th>آخر زيارة</th><th>الزيارات</th><th>الصفحات</th><th>الجهاز</th><th>IP</th><th>جاء من</th></tr></thead>
                                    <tbody id="visPeople"></tbody>
                                </table>
                            </div>
                        </div>
                        <div class="bill-card bill-wide">
                            <h3>آخر الزيارات</h3>
                            <div class="table-wrap">
                                <table class="bill-table">
                                    <thead><tr><th>الوقت</th><th>الزائر</th><th>الصفحة</th><th>جاء من</th><th>الجهاز</th><th>IP</th></tr></thead>
                                    <tbody id="visRecent"></tbody>
                                </table>
                            </div>
                        </div>
                        <p class="vis-note">يُعرَف الزائر برقم ثابت فقط إذا وافق على الكوكيز. الزائر الذي لم يوافق تُحسب زيارته دون كوكيز ويُخفى آخر جزء من عنوان IP الخاص به. زيارات فريق الإدارة لا تُحسب.</p>
                    </section>

                    <template id="billInfoTpl">
                        <div class="bill-pop" role="tooltip">
                            <strong>ما الذي تشمله الإدامة الشهرية؟</strong>
                            <ul>
                                <li><b>الخادم والاستضافة:</b> كل زيارة للموقع تستهلك موارد الخادم، والخادم يُدفع له اشتراك شهري ليبقى الموقع متاحاً وسريعاً على مدار الساعة.</li>
                                <li><b>عمل المطوّرين:</b> فريق تطوير يتابع الموقع باستمرار، يعالج أي ملاحظة ويضيف ما يلزم.</li>
                                <li><b>المراقبة والتحسين:</b> أدوات مراقبة تتابع أداء الموقع وزياراته، لنعرف ما يحتاج إلى تحسين ونطوّره.</li>
                                <li><b>التحديث المستمر:</b> الموقع يُحدَّث ويتطوّر شهراً بعد شهر ليبقى حديثاً وآمناً.</li>
                                <li><b>التقرير الشهري:</b> تقرير كل شهر بالزيارات والأداء وما تم إنجازه، لتطّلع على قيمة ما تحصل عليه.</li>
                            </ul>
                        </div>
                    </template>

                    <!-- ===== BILLING TAB ===== -->
                    <section id="tab-billing" class="tab-panel">
                        <div class="bill-layout">
                            <article class="invoice" id="invoiceCard">
                                <header class="invoice-head">
                                    <div>
                                        <div class="invoice-kicker">فاتورة</div>
                                        <h2 class="invoice-title">الإدامة الشهرية للموقع <button type="button" class="bill-info" aria-label="لماذا هذه الفاتورة؟" onclick="toggleBillInfo(event, this)">!</button></h2>
                                        <div class="invoice-sub">ورشة الفرات الأوسط — ME Workshop</div>
                                        <div class="invoice-essential">تضمن عمل الموقع بكفاءة وأمان، وتطويره وتحسينه شهراً بعد شهر</div>
                                    </div>
                                    <div class="invoice-status" id="invStatus"></div>
                                </header>
                                <dl class="invoice-meta">
                                    <div><dt>رقم الفاتورة</dt><dd id="invNumber" class="ltr">-</dd></div>
                                    <div><dt>فترة الإدامة</dt><dd id="invPeriod">-</dd></div>
                                    <div><dt>تاريخ الاستحقاق</dt><dd id="invDue">-</dd></div>
                                    <div><dt>الاستحقاق القادم</dt><dd id="invNext">-</dd></div>
                                </dl>
                                <table class="invoice-lines">
                                    <tbody>
                                        <tr><td>رسوم الإدامة الشهرية للموقع <button type="button" class="bill-info" aria-label="لماذا هذه الفاتورة؟" onclick="toggleBillInfo(event, this)">!</button></td><td class="num" id="invMonthly">-</td></tr>
                                        <tr><td>رصيد سابق غير مسدد</td><td class="num" id="invPrev">-</td></tr>
                                        <tr class="credit-row" id="invCreditRow" hidden><td>رصيد دائن (مدفوع مقدماً)</td><td class="num" id="invCredit">-</td></tr>
                                    </tbody>
                                    <tfoot>
                                        <tr><td>المبلغ المستحق</td><td class="num" id="invTotal">-</td></tr>
                                    </tfoot>
                                </table>
                                <p class="invoice-note">تُستحق رسوم الإدامة في اليوم <span id="invDay" class="ltr">13</span> من كل شهر وتتجدد تلقائياً. أي مبلغ غير مسدد يُرحَّل إلى الفاتورة التالية.</p>
                            </article>

                            <aside class="bill-side">
                                <div class="bill-card" id="payFormCard" hidden>
                                    <h3>تسجيل دفعة</h3>
                                    <label>المبلغ (USD)<input type="number" id="payAmount" min="0.01" step="0.01" inputmode="decimal" /></label>
                                    <label>تاريخ الدفع<input type="date" id="payDate" /></label>
                                    <label>ملاحظة (اختياري)<input type="text" id="payNote" maxlength="200" placeholder="مثال: نقداً / تحويل" /></label>
                                    <button class="bill-btn" id="payBtn" onclick="recordPayment()">تسجيل الدفعة</button>
                                    <div class="bill-msg" id="payMsg"></div>
                                </div>
                                <div class="bill-card">
                                    <h3>ملخص الحساب</h3>
                                    <dl class="bill-totals">
                                        <div><dt>إجمالي الفواتير</dt><dd id="sumBilled" class="ltr">-</dd></div>
                                        <div><dt>إجمالي المدفوع</dt><dd id="sumPaid" class="ltr">-</dd></div>
                                        <div><dt>الرصيد المستحق</dt><dd id="sumBalance" class="ltr">-</dd></div>
                                    </dl>
                                </div>
                            </aside>
                        </div>

                        <div class="bill-card bill-wide">
                            <h3>سجل الدفعات</h3>
                            <div class="table-wrap">
                                <table class="bill-table">
                                    <thead><tr><th>التاريخ</th><th>المبلغ</th><th>ملاحظة</th><th>سُجّلت بواسطة</th><th class="admin-only-col"></th></tr></thead>
                                    <tbody id="payRows"></tbody>
                                </table>
                            </div>
                        </div>

                        <div class="bill-card bill-wide">
                            <h3>سجل الفواتير</h3>
                            <div class="table-wrap">
                                <table class="bill-table">
                                    <thead><tr><th>رقم الفاتورة</th><th>الفترة</th><th>المبلغ</th><th>المدفوع</th><th>المتبقي</th><th>الحالة</th></tr></thead>
                                    <tbody id="invRows"></tbody>
                                </table>
                            </div>
                        </div>
                    </section>

                    <!-- ===== EXPLORER CARDS TAB ===== -->
                    <section id="tab-explorer" class="tab-panel">
                        <fieldset>
                            <legend>إدارة بطاقات المستكشف</legend>
                            <p style="color: #999; font-size: 0.82rem; margin-bottom: 16px;">
                                أضف وعدّل بطاقات التقنيات والقدرات المعروضة في صفحة "المستكشف".
                            </p>

                            <div class="editing-banner" id="explorerEditingBanner">
                                <span id="explorerEditingLabel">جاري تعديل بطاقة...</span>
                                <button onclick="cancelExplorerEdit()">إلغاء التعديل</button>
                            </div>

                            <div class="archival-form-grid">
                                <input type="text" id="explorerIcon" placeholder="الأيقونة (اختياري)" value="" />
                                <input type="text" id="explorerImageUrl" placeholder="رابط الصورة" />
                                <input type="text" id="explorerTitleAr" placeholder="العنوان بالعربية" />
                                <input type="text" id="explorerTitleEn" placeholder="Title in English" />
                                <input type="text" id="explorerTitleKu" placeholder="ناونیشان بە کوردی" />
                                <textarea id="explorerDescAr" placeholder="الوصف بالعربية"></textarea>
                                <textarea id="explorerDescEn" placeholder="Description in English"></textarea>
                                <textarea id="explorerDescKu" placeholder="وەسف بە کوردی"></textarea>
                                <input type="text" id="explorerTags" placeholder="الوسوم (مفصولة بفاصلة) مثال: دقة عالية, تصنيع رقمي" />
                                <input type="text" id="explorerLink" placeholder="الرابط (مثال: /guide)" value="/guide" />
                                <input type="text" id="explorerSortOrder" placeholder="ترتيب العرض (رقم)" value="0" />
                            </div>
                            <div style="margin-bottom: 12px;">
                                <input type="file" id="explorerImageUploadInput" accept="image/*" style="color: #ccc; font-size: 0.8rem;" />
                                <button class="small-btn primary" onclick="uploadExplorerImage()">رفع صورة</button>
                                <span id="explorerUploadResult" style="font-size: 0.78rem; color: #9fd39f; word-break: break-all;"></span>
                            </div>
                            <button class="small-btn primary" id="explorerSubmitBtn" onclick="submitExplorerCard()">إضافة بطاقة جديدة</button>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>البطاقات الحالية</legend>
                            <div class="table-wrap">
                                <table>
                                    <thead>
                                        <tr>
                                            <th>الأيقونة</th><th>الصورة</th><th>العنوان (عربي)</th>
                                            <th>الوسوم</th><th>الترتيب</th><th>الإجراءات</th>
                                        </tr>
                                    </thead>
                                    <tbody id="explorerCardsBody"></tbody>
                                </table>
                            </div>
                        </fieldset>
                    </section>

                    <!-- ===== GUIDE CONTENT TAB ===== -->
                    <section id="tab-guide" class="tab-panel">
                        <fieldset>
                            <legend>إدارة محتوى الدليل الشامل — المواضيع</legend>
                            <div class="editing-banner" id="guideTopicEditingBanner">
                                <span id="guideTopicEditingLabel">جاري تعديل موضوع...</span>
                                <button onclick="cancelGuideTopicEdit()">إلغاء التعديل</button>
                            </div>
                            <div class="archival-form-grid">
                                <input type="text" id="guideTopicIcon" placeholder="الأيقونة (اختياري)" value="" />
                                <input type="text" id="guideTopicSortOrder" placeholder="ترتيب العرض (رقم)" value="0" />
                                <input type="text" id="guideTopicTitleAr" placeholder="العنوان بالعربية" />
                                <input type="text" id="guideTopicTitleEn" placeholder="Title in English" />
                                <input type="text" id="guideTopicTitleKu" placeholder="ناونیشان بە کوردی" />
                                <textarea id="guideTopicBodyAr" placeholder="المحتوى بالعربية"></textarea>
                                <textarea id="guideTopicBodyEn" placeholder="Body in English"></textarea>
                                <textarea id="guideTopicBodyKu" placeholder="ناوەڕۆک بە کوردی"></textarea>
                            </div>
                            <button class="small-btn primary" id="guideTopicSubmitBtn" onclick="submitGuideTopic()">إضافة موضوع جديد</button>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>المواضيع الحالية</legend>
                            <div class="table-wrap">
                                <table>
                                    <thead>
                                        <tr><th>الأيقونة</th><th>العنوان (عربي)</th><th>الترتيب</th><th>الإجراءات</th></tr>
                                    </thead>
                                    <tbody id="guideTopicsBody"></tbody>
                                </table>
                            </div>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>إدارة معدات الدليل</legend>
                            <div class="editing-banner" id="guideEquipEditingBanner">
                                <span id="guideEquipEditingLabel">جاري تعديل معدة...</span>
                                <button onclick="cancelGuideEquipEdit()">إلغاء التعديل</button>
                            </div>
                            <div class="archival-form-grid">
                                <input type="text" id="guideEquipNameAr" placeholder="اسم المعدة بالعربية" />
                                <input type="text" id="guideEquipNameEn" placeholder="Equipment name in English" />
                                <input type="text" id="guideEquipNameKu" placeholder="ناوی ئامێر بە کوردی" />
                                <input type="text" id="guideEquipSpecAr" placeholder="المواصفات (عربي)" />
                                <input type="text" id="guideEquipSpecEn" placeholder="Specifications (English)" />
                                <input type="text" id="guideEquipSpecKu" placeholder="تایبەتمەندی (کوردی)" />
                                <input type="text" id="guideEquipUseAr" placeholder="الاستخدام (عربي)" />
                                <input type="text" id="guideEquipUseEn" placeholder="Usage (English)" />
                                <input type="text" id="guideEquipUseKu" placeholder="بەکارهێنان (کوردی)" />
                                <input type="text" id="guideEquipSortOrder" placeholder="ترتيب العرض (رقم)" value="0" />
                            </div>
                            <button class="small-btn primary" id="guideEquipSubmitBtn" onclick="submitGuideEquip()">إضافة معدة جديدة</button>
                        </fieldset>

                        <fieldset style="margin-top: 20px;">
                            <legend>المعدات الحالية</legend>
                            <div class="table-wrap">
                                <table>
                                    <thead>
                                        <tr><th>الاسم (عربي)</th><th>المواصفات (عربي)</th><th>الترتيب</th><th>الإجراءات</th></tr>
                                    </thead>
                                    <tbody id="guideEquipBody"></tbody>
                                </table>
                            </div>
                        </fieldset>
                    </section>
                </div>

                <!-- ===== DIRECT PIN DROP MODAL (Fullscreen Map Modal) ===== -->
                <div class="admin-modal-overlay" id="pinModalOverlay">
                    <div class="admin-modal-box">
                        <button class="modal-close" onclick="closePinModal()">×</button>
                        <h2>إضافة موقع جديد على الخريطة</h2>
                        <div class="modal-map-wrap">
                            <div id="modalAdminMap"></div>
                        </div>
                        <div class="coords-display" id="modalCoordsDisplay">خط العرض: — | خط الطول: —</div>
                        <div class="form-group">
                            <label>اسم القسم / الورشة</label>
                            <input type="text" id="modalPinName" placeholder="مثال: قسم تصليح البمبات" />
                        </div>
                        <div class="form-group">
                            <label>العنوان</label>
                            <input type="text" id="modalPinAddress" placeholder="النجف الأشرف - الحي الصناعي..." />
                        </div>
                        <div class="form-group">
                            <label>رقم الهاتف</label>
                            <input type="text" id="modalPinPhone" placeholder="07xx xxx xxxx" />
                        </div>
                        <div class="modal-actions">
                            <button class="btn-confirm" onclick="confirmDirectPin()">تأكيد وإضافة الموقع</button>
                            <button class="btn-cancel" onclick="closePinModal()">إلغاء</button>
                        </div>
                    </div>
                </div>

                <link rel="stylesheet" href="/me/leaflet/leaflet.css" />
                <script src="/me/leaflet/leaflet.js"></script>
                <script src="/me/maps.js?v=%%MAPSV%%"></script>

                <script>
                var contentData = { translations: { ar: {}, en: {}, ku: {} }, heroImages: [] };
                var industrialData = { translations: { ar: {}, en: {}, ku: {} }, mapEmbedUrl: '' };
                var editingPinId = null;
                var editingAlbumId = null;
                var editingExplorerId = null;
                var editingGuideTopicId = null;
                var editingGuideEquipId = null;

                // Admin map state
                var adminMap = null;
                var adminMarkers = {};
                var adminBaseLayerGroup = null;
                var adminLayerControl = null;
                var adminPendingLayerGroup = null;
                var tempPinMarker = null;
                var pendingPinCoords = null;

                // Modal map state
                var modalAdminMap = null;
                var modalAdminMarker = null;

                // =========================================================
                // ============ AUTHENTICATION & ACCOUNTS MANAGEMENT =======
                // =========================================================
                var currentUser = null;

                async function checkAuth() {
                    try {
                        var res = await fetch('/api/auth/me');
                        if (res.status === 401) { showLoginGate(); return; }
                        var data = await res.json();
                        currentUser = data.user;
                        showDashboard();
                    } catch (e) { showLoginGate(); }
                }

                function showLoginGate() {
                    document.getElementById('loginGate').classList.remove('hidden');
                    document.getElementById('dashboard').classList.add('hidden');
                    var card = document.getElementById('createAccountCard');
                    if (card) card.style.display = 'none';
                    var info = document.getElementById('headerAccountInfo');
                    if (info) info.style.display = 'none';
                }

                function showDashboard() {
                    document.getElementById('loginGate').classList.add('hidden');
                    document.getElementById('dashboard').classList.remove('hidden');

                    renderAccountStatusBar();

                    var card = document.getElementById('createAccountCard');
                    if (card) { card.style.display = (currentUser.role === 'Admin') ? 'block' : 'none'; }

                    loadAccounts();
                    loadRequests();
                    loadContent();
                    loadIndustrialContent();
                    loadMapPins();
                    loadPendingPins();
                    loadAlbumItems();
                    loadExplorerCardsAdmin();
                    loadGuideContentAdmin();
                    initAdminMap();
                    loadGalleryAdmin();
                    loadBilling();
                    loadVisitors();
                    if (currentUser && currentUser.role === 'Manager') switchAdminTab('gallery');
                }

                function renderAccountStatusBar() {
                    if (!currentUser) return;

                    // إظهار بيانات الحساب في الـ Header
                    var info = document.getElementById('headerAccountInfo');
                    if (info) info.style.display = 'flex';

                    var label = document.getElementById('headerAccountLabel');
                    if (label) label.textContent = currentUser.email + ' (' + currentUser.role + ')';

                    var badge = document.getElementById('headerRoleBadge');
                    if (badge) {
                        if (currentUser.role === 'Admin') {
                            badge.textContent = 'Admin';
                            badge.style.background = 'rgba(212,175,55,0.15)';
                            badge.style.border = '1px solid #D4AF37';
                            badge.style.color = '#D4AF37';
                        } else {
                            badge.textContent = 'Manager';
                            badge.style.background = 'rgba(107,114,128,0.15)';
                            badge.style.border = '1px solid #6b7280';
                            badge.style.color = '#9ca3af';
                        }
                    }
                }

                async function adminLogin() {
                    var email = document.getElementById('loginEmailInput').value.trim();
                    var password = document.getElementById('loginPasswordInput').value;
                    var errEl = document.getElementById('loginError');
                    if (!email || !password) { errEl.textContent = 'البريد وكلمة المرور مطلوبان'; return; }
                    try {
                        var res = await fetch('/api/auth/login', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ email: email, password: password })
                        });
                        var data = await res.json();
                        if (res.ok && data.success) {
                            errEl.textContent = '';
                            currentUser = data.user;
                            showDashboard();
                        } else {
                            errEl.textContent = data.message || 'فشل تسجيل الدخول';
                        }
                    } catch (e) { errEl.textContent = 'خطأ في الاتصال بالخادم'; }
                }

                async function adminLogout() {
                    try { await fetch('/api/auth/logout', { method: 'POST' }); } catch (e) { }
                    window.location.reload();
                }

                // =========================================================
                // ==================== ACCOUNTS TABLE =====================
                // =========================================================
                async function loadAccounts() {
                    try {
                        var res = await fetch('/api/admin/accounts');
                        if (!res.ok) return;
                        var accounts = await res.json();
                        var body = document.getElementById('accountsBody');
                        body.innerHTML = '';
                        document.getElementById('accountsCount').textContent = accounts.length;
                        var isAdmin = currentUser && currentUser.role === 'Admin';
                        accounts.forEach(function(acc) {
                            var tr = document.createElement('tr');
                            var created = acc.createdAt ? new Date(acc.createdAt).toLocaleString() : '-';
                            var roleLabel = acc.role === 'Admin'
                                ? '<span style="color:#D4AF37;font-weight:700;">Admin </span>'
                                : '<span style="color:#9ca3af;font-weight:700;">Manager </span>';
                            var verifiedLabel = acc.isVerified
                                ? '<span style="color:#4ade80;">موثّق</span>'
                                : '<span style="color:#ffb86b;">غير موثّق</span>';
                            var actionsHtml = '';
                            if (isAdmin) {
                                var emailEscaped = (acc.email || '').replace(/'/g, "\\'");
                                actionsHtml =
                                    '<button class="small-btn primary" onclick="changeAccountPassword(' + acc.id + ', \'' + emailEscaped + '\')">تغيير الباسورد</button>' +
                                    '<button class="small-btn danger" onclick="deleteAccount(' + acc.id + ', \'' + emailEscaped + '\')">حذف</button>';
                            } else {
                                actionsHtml = '<span style="color:#666;font-size:0.8rem;">— للعرض فقط —</span>';
                            }
                            tr.innerHTML =
                                '<td>' + escapeHtml(acc.fullName || '-') + '</td>' +
                                '<td>' + escapeHtml(acc.email || '-') + '</td>' +
                                '<td>' + roleLabel + '</td>' +
                                '<td>' + verifiedLabel + '</td>' +
                                '<td>' + created + '</td>' +
                                '<td>' + actionsHtml + '</td>';
                            body.appendChild(tr);
                        });
                    } catch (e) { }
                }

                async function createNewAccount() {
                    var fullName = document.getElementById('newAccountFullName').value.trim();
                    var email = document.getElementById('newAccountEmail').value.trim();
                    var password = document.getElementById('newAccountPassword').value;
                    var role = document.getElementById('newAccountRole').value;
                    var msg = document.getElementById('createAccountMsg');
                    if (!fullName || !email || !password) {
                        msg.textContent = 'جميع الحقول مطلوبة';
                        msg.style.color = '#ff6b6b';
                        return;
                    }
                    try {
                        var res = await fetch('/api/admin/accounts', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ fullName: fullName, email: email, password: password, role: role })
                        });
                        var data = await res.json();
                        if (res.ok && data.success) {
                            msg.textContent = 'تم إنشاء الحساب بنجاح';
                            msg.style.color = '#4ade80';
                            document.getElementById('newAccountFullName').value = '';
                            document.getElementById('newAccountEmail').value = '';
                            document.getElementById('newAccountPassword').value = '';
                            document.getElementById('newAccountRole').value = 'Manager';
                            loadAccounts();
                        } else {
                            msg.textContent = data.message || 'فشل الإنشاء';
                            msg.style.color = '#ff6b6b';
                        }
                    } catch (e) { msg.textContent = 'خطأ في الاتصال'; msg.style.color = '#ff6b6b'; }
                }

                async function changeAccountPassword(userId, email) {
                    var newPass = prompt('أدخل كلمة المرور الجديدة للحساب: ' + email + '\n(6 أحرف على الأقل)');
                    if (newPass === null) return;
                    if (!newPass || newPass.length < 6) { alert('كلمة المرور يجب أن تكون 6 أحرف على الأقل'); return; }
                    try {
                        var res = await fetch('/api/admin/accounts/' + userId + '/password', {
                            method: 'PUT',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ userId: userId, newPassword: newPass })
                        });
                        var data = await res.json();
                        if (res.ok && data.success) { alert('تم تغيير كلمة المرور بنجاح'); }
                        else { alert(data.message || 'فشل تغيير كلمة المرور'); }
                    } catch (e) { alert('خطأ في الاتصال'); }
                }

                async function deleteAccount(userId, email) {
                    if (!confirm('هل أنت متأكد من حذف الحساب: ' + email + '؟\nهذا الإجراء لا يمكن التراجع عنه.')) return;
                    try {
                        var res = await fetch('/api/admin/accounts/' + userId, { method: 'DELETE' });
                        var data = await res.json();
                        if (res.ok && data.success) { alert('تم حذف الحساب'); loadAccounts(); }
                        else { alert(data.message || 'فشل الحذف'); }
                    } catch (e) { alert('خطأ في الاتصال'); }
                }

                function switchAdminTab(tab) {
                    document.querySelectorAll('.admin-tabs .tab-btn').forEach(function(btn) {
                        btn.classList.toggle('active', btn.dataset.tab === tab);
                    });
                    document.querySelectorAll('.tab-panel').forEach(function(panel) {
                        panel.classList.toggle('active', panel.id === 'tab-' + tab);
                    });
                    var activeBtn = document.querySelector('.admin-tabs .tab-btn.active');
                    if (activeBtn && activeBtn.scrollIntoView) activeBtn.scrollIntoView({ block: 'nearest', inline: 'center' });
                    if (tab === 'industrial' && adminMap) {
                        setTimeout(function() { adminMap.invalidateSize(); }, 150);
                    }
                }

                // ---- Accordion toggle ----
                function toggleAccordion(id) {
                    var panel = document.getElementById(id);
                    if (panel) panel.classList.toggle('collapsed');
                }

                // ---- Users ----
                function loadUsers() {
                    fetch('/api/admin/users').then(function(r) { return r.json(); }).then(function(users) {
                        document.getElementById('usersCount').textContent = users.length;
                        var body = document.getElementById('usersBody');
                        body.innerHTML = '';
                        users.forEach(function(u) {
                            var tr = document.createElement('tr');
                            var registered = u.registeredAt ? new Date(u.registeredAt).toLocaleString() : '-';
                            var lastLogin = u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : '-';
                            tr.innerHTML =
                                '<td>' + escapeHtml(u.username) + '</td>' +
                                '<td>' + escapeHtml(u.email || '-') + '</td>' +
                                '<td>' + registered + '</td>' +
                                '<td>' + escapeHtml(u.status) + '</td>' +
                                '<td>' + lastLogin + '</td>' +
                                '<td><button class="small-btn danger" onclick="deleteUser(\'' + u.id + '\')">حذف</button></td>';
                            body.appendChild(tr);
                        });
                    });
                }

                function deleteUser(id) {
                    if (!confirm('هل أنت متأكد من حذف هذا الحساب؟')) return;
                    fetch('/api/admin/users/' + id, { method: 'DELETE' }).then(function() { loadUsers(); });
                }

                // ---- Service requests ----
                function loadRequests() {
                    fetch('/api/admin/service-requests').then(function(r) { return r.json(); }).then(function(items) {
                        var body = document.getElementById('requestsBody');
                        body.innerHTML = '';
                        items.forEach(function(req) {
                            var submitted = req.submittedAt ? new Date(req.submittedAt).toLocaleString() : '-';
                            var tr = document.createElement('tr');
                            tr.innerHTML =
                                '<td>' + escapeHtml(req.fullName) + '</td>' +
                                '<td>' + escapeHtml(req.phone) + '</td>' +
                                '<td>' + escapeHtml(req.serviceType) + '</td>' +
                                '<td style="max-width:260px; white-space:normal;">' + escapeHtml(req.message || '-') + '</td>' +
                                '<td>' + submitted + '</td>' +
                                '<td>' +
                                    '<select class="status-select" id="status-' + req.id + '">' +
                                        '<option value="Pending"' + (req.status === 'Pending' ? ' selected' : '') + '>قيد الانتظار</option>' +
                                        '<option value="InProgress"' + (req.status === 'InProgress' ? ' selected' : '') + '>قيد التنفيذ</option>' +
                                        '<option value="Completed"' + (req.status === 'Completed' ? ' selected' : '') + '>مكتمل</option>' +
                                    '</select>' +
                                '</td>' +
                                '<td>' +
                                    '<button class="small-btn primary" onclick="updateRequestStatus(\'' + req.id + '\')">حفظ</button>' +
                                    '<button class="small-btn danger" onclick="deleteRequest(\'' + req.id + '\')">حذف</button>' +
                                '</td>';
                            body.appendChild(tr);
                        });
                    });
                }

                function updateRequestStatus(id) {
                    var status = document.getElementById('status-' + id).value;
                    fetch('/api/admin/service-requests/' + id, {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ status: status })
                    }).then(function() { loadRequests(); });
                }

                function deleteRequest(id) {
                    if (!confirm('هل أنت متأكد من حذف هذا الطلب؟')) return;
                    fetch('/api/admin/service-requests/' + id, { method: 'DELETE' }).then(function() { loadRequests(); });
                }

                // ---- Home content ----
                function buildFields(containerId, langObj) {
                    var container = document.getElementById(containerId);
                    container.innerHTML = '';
                    Object.keys(langObj).sort().forEach(function(key) {
                        var wrap = document.createElement('div');
                        wrap.className = 'field-row';
                        var label = document.createElement('label');
                        label.textContent = key;
                        var input = document.createElement('textarea');
                        input.rows = 2;
                        input.dataset.key = key;
                        input.value = langObj[key];
                        wrap.appendChild(label);
                        wrap.appendChild(input);
                        container.appendChild(wrap);
                    });
                }

                function collectFields(containerId) {
                    var container = document.getElementById(containerId);
                    var result = {};
                    container.querySelectorAll('textarea').forEach(function(ta) { result[ta.dataset.key] = ta.value; });
                    return result;
                }

                function loadContent() {
                    fetch('/api/content').then(function(r) { return r.json(); }).then(function(data) {
                        contentData = data || { translations: { ar: {}, en: {}, ku: {} }, heroImages: [] };
                        ['ar', 'en', 'ku'].forEach(function(l) {
                            buildFields('contentFields-' + l, contentData.translations[l] || {});
                        });
                        var imgs = contentData.heroImages || [];
                        for (var i = 0; i < 4; i++) {
                            var el = document.getElementById('heroImage' + i);
                            if (el) el.value = imgs[i] || '';
                        }
                    });
                }

                function switchContentLang(lang) {
                    document.querySelectorAll('#tab-content .lang-tab-btn').forEach(function(btn) {
                        btn.classList.toggle('active', btn.dataset.lang === lang);
                    });
                    ['ar', 'en', 'ku'].forEach(function(l) {
                        document.getElementById('contentFields-' + l).classList.toggle('hidden', l !== lang);
                    });
                }

                function saveContent() {
                    var translations = { ar: collectFields('contentFields-ar'), en: collectFields('contentFields-en'), ku: collectFields('contentFields-ku') };
                    var heroImages = [];
                    for (var i = 0; i < 4; i++) {
                        var el = document.getElementById('heroImage' + i);
                        heroImages.push(el ? el.value : '');
                    }
                    fetch('/api/admin/content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ translations: translations, heroImages: heroImages })
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        alert(res.success ? 'تم حفظ محتوى الصفحة الرئيسية بنجاح' : 'حدث خطأ أثناء الحفظ');
                    }).catch(function() { alert('حدث خطأ أثناء الحفظ'); });
                }

                // ---- Industrial content ----
                function loadIndustrialContent() {
                    fetch('/api/industrial-content').then(function(r) { return r.json(); }).then(function(data) {
                        industrialData = data || { translations: { ar: {}, en: {}, ku: {} }, mapEmbedUrl: '' };
                        ['ar', 'en', 'ku'].forEach(function(l) {
                            buildFields('indFields-' + l, industrialData.translations[l] || {});
                        });
                        document.getElementById('mapEmbedUrlInput').value = industrialData.mapEmbedUrl || '';
                    });
                }

                function switchIndLang(lang) {
                    document.querySelectorAll('#tab-industrial .lang-tab-btn').forEach(function(btn) {
                        btn.classList.toggle('active', btn.dataset.lang === lang);
                    });
                    ['ar', 'en', 'ku'].forEach(function(l) {
                        document.getElementById('indFields-' + l).classList.toggle('hidden', l !== lang);
                    });
                }

                function saveIndustrialContent() {
                    var translations = { ar: collectFields('indFields-ar'), en: collectFields('indFields-en'), ku: collectFields('indFields-ku') };
                    var mapEmbedUrl = document.getElementById('mapEmbedUrlInput').value;
                    fetch('/api/admin/industrial-content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ translations: translations, mapEmbedUrl: mapEmbedUrl })
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        alert(res.success ? 'تم حفظ تغييرات الخدمات الصناعية بنجاح' : 'حدث خطأ أثناء الحفظ');
                    }).catch(function() { alert('حدث خطأ أثناء الحفظ'); });
                }

                // ---- ADMIN MAP ----
                function createGoogleLayer(layerType, label) {
                    return MEMaps.layer(layerType, label);
                }

                var adminGoogleRoadmap = createGoogleLayer('m', 'خريطة الشوارع');
                var adminGoogleSatellite = createGoogleLayer('s', 'الصور الفضائية');
                var adminGoogleHybrid = createGoogleLayer('y', 'الخريطة الهجينة');
                var adminGoogleTerrain = createGoogleLayer('p', 'التضاريس');

                function initAdminMap() {
                    if (adminMap) {
                        setTimeout(function() { adminMap.invalidateSize(); }, 300);
                        return;
                    }
                    adminMap = L.map('adminMap', {
                        center: MEMaps.center,
                        zoom: 16,
                        layers: [adminGoogleRoadmap]
                    });

                    var baseMaps = {
                        'خريطة الشوارع': adminGoogleRoadmap,
                        'الصور الفضائية': adminGoogleSatellite,
                        'الخريطة الهجينة': adminGoogleHybrid,
                        'التضاريس': adminGoogleTerrain
                    };
                    adminLayerControl = L.control.layers(baseMaps, null, {
                        position: 'topright',
                        collapsed: true
                    }).addTo(adminMap);

                    adminBaseLayerGroup = L.layerGroup().addTo(adminMap);
                    adminPendingLayerGroup = L.layerGroup().addTo(adminMap);

                    // Click on map → open fullscreen pin modal
                    adminMap.on('click', function(e) {
                        openPinModal(e.latlng.lat, e.latlng.lng);
                    });

                    setTimeout(function() { adminMap.invalidateSize(); }, 300);
                }

                // ---- Fullscreen Map Modal for Direct Pin Drop ----
                window.openPinModal = function(lat, lng) {
                    pendingPinCoords = { lat: lat, lng: lng };
                    document.getElementById('modalCoordsDisplay').textContent =
                        'خط العرض: ' + lat.toFixed(6) + ' | خط الطول: ' + lng.toFixed(6);
                    document.getElementById('modalPinName').value = '';
                    document.getElementById('modalPinAddress').value = '';
                    document.getElementById('modalPinPhone').value = '';
                    document.getElementById('pinModalOverlay').classList.add('active');
                    document.body.style.overflow = 'hidden';

                    // Initialize or update modal map
                    setTimeout(function() {
                        if (!modalAdminMap) {
                            modalAdminMap = L.map('modalAdminMap', {
                                center: [lat, lng],
                                zoom: 15,
                                layers: [adminGoogleRoadmap]
                            });

                            var modalBaseMaps = {
                                'خريطة الشوارع': adminGoogleRoadmap,
                                'الصور الفضائية': adminGoogleSatellite,
                                'الخريطة الهجينة': adminGoogleHybrid,
                                'التضاريس': adminGoogleTerrain
                            };
                            L.control.layers(modalBaseMaps, null, {
                                position: 'topright',
                                collapsed: true
                            }).addTo(modalAdminMap);

                            modalAdminMap.on('click', function(e) {
                                if (modalAdminMarker) {
                                    modalAdminMarker.setLatLng(e.latlng);
                                } else {
                                    modalAdminMarker = L.marker(e.latlng, { draggable: true }).addTo(modalAdminMap);
                                    modalAdminMarker.on('dragend', function() {
                                        var pos = modalAdminMarker.getLatLng();
                                        pendingPinCoords = { lat: pos.lat, lng: pos.lng };
                                        document.getElementById('modalCoordsDisplay').textContent =
                                            'خط العرض: ' + pos.lat.toFixed(6) + ' | خط الطول: ' + pos.lng.toFixed(6);
                                    });
                                }
                                pendingPinCoords = { lat: e.latlng.lat, lng: e.latlng.lng };
                                document.getElementById('modalCoordsDisplay').textContent =
                                    'خط العرض: ' + e.latlng.lat.toFixed(6) + ' | خط الطول: ' + e.latlng.lng.toFixed(6);
                                modalAdminMap.setView(e.latlng, 16);
                            });
                        } else {
                            modalAdminMap.setView([lat, lng], 15);
                        }

                        // Add or move marker
                        if (modalAdminMarker) {
                            modalAdminMarker.setLatLng([lat, lng]);
                        } else {
                            modalAdminMarker = L.marker([lat, lng], { draggable: true }).addTo(modalAdminMap);
                            modalAdminMarker.on('dragend', function() {
                                var pos = modalAdminMarker.getLatLng();
                                pendingPinCoords = { lat: pos.lat, lng: pos.lng };
                                document.getElementById('modalCoordsDisplay').textContent =
                                    'خط العرض: ' + pos.lat.toFixed(6) + ' | خط الطول: ' + pos.lng.toFixed(6);
                            });
                        }

                        // CRITICAL: invalidateSize after modal opens
                        setTimeout(function() {
                            modalAdminMap.invalidateSize();
                            window.dispatchEvent(new Event('resize'));
                        }, 200);
                    }, 100);
                };

                window.closePinModal = function() {
                    document.getElementById('pinModalOverlay').classList.remove('active');
                    document.body.style.overflow = '';
                    if (modalAdminMarker) {
                        modalAdminMap.removeLayer(modalAdminMarker);
                        modalAdminMarker = null;
                    }
                    pendingPinCoords = null;
                };

                window.confirmDirectPin = function() {
                    var name = document.getElementById('modalPinName').value.trim();
                    var address = document.getElementById('modalPinAddress').value.trim();
                    var phone = document.getElementById('modalPinPhone').value.trim();
                    if (!name) { alert('الرجاء إدخال اسم القسم'); return; }
                    if (!pendingPinCoords) { alert('الرجاء النقر على الخريطة أولاً'); return; }

                    var payload = {
                        nameAr: name,
                        nameEn: name,
                        nameKu: name,
                        addressAr: address || 'النجف الأشرف - الحي الصناعي',
                        addressEn: address || 'Najaf Al-Ashraf - Industrial District',
                        addressKu: address || 'نەجەف ئەشرەف - گەڕەکی پیشەسازی',
                        phone: phone,
                        latitude: pendingPinCoords.lat,
                        longitude: pendingPinCoords.lng,
                        mapEmbedUrl: ''
                    };

                    fetch('/api/admin/map-pins', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) {
                            closePinModal();
                            loadMapPins();
                            loadPendingPins();
                        } else {
                            alert('حدث خطأ أثناء إضافة الموقع');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء إضافة الموقع'); });
                };

                // ---- Map pins / departments (admin) ----
                function loadMapPins() {
                    fetch('/api/admin/map-pins/approved').then(function(r) { return r.ok ? r.json() : []; }).then(function(pins) {
                        var body = document.getElementById('mapPinsBody');
                        body.innerHTML = '';
                        document.getElementById('approvedCount').textContent = (pins || []).length;
                        (pins || []).forEach(function(pin) {
                            var tr = document.createElement('tr');
                            if (pin.isWorkshop) tr.className = 'ws-admin-row';
                            tr.innerHTML =
                                '<td>' + (pin.isWorkshop ? '<span class="ws-admin-tag">الورشة — الموقع الرئيسي</span><br>' : '') + escapeHtml(pin.nameAr || '-') + '</td>' +
                                '<td>' + escapeHtml(pin.nameEn || '-') + '</td>' +
                                '<td>' + escapeHtml(pin.addressAr || '-') + '</td>' +
                                '<td>' + escapeHtml(pin.phone || '-') + '</td>' +
                                '<td>' + escapeHtml(String(pin.latitude)) + '</td>' +
                                '<td>' + escapeHtml(String(pin.longitude)) + '</td>' +
                                '<td>' +
                                    '<button class="small-btn primary" onclick=\'startEditPin(' + JSON.stringify(pin) + ')\'>تعديل</button>' +
                                    (pin.isWorkshop ? '' : '<button class="small-btn danger" onclick="deleteMapPin(\'' + pin.id + '\')">حذف</button>') +
                                '</td>';
                            body.appendChild(tr);

                            // Add marker to admin map
                            if (adminBaseLayerGroup) {
                                if (adminMarkers[pin.id]) { adminMarkers[pin.id].remove(); }
                                var popupContent = '<strong>' + escapeHtml(pin.nameAr || pin.nameEn) + '</strong><br>' + escapeHtml(pin.addressAr || '') + '<br>' + escapeHtml(pin.phone || '-');
                                var marker = pin.isWorkshop
                                    ? MEMaps.addWorkshop(adminBaseLayerGroup, pin.latitude, pin.longitude, pin.nameAr || pin.nameEn)
                                    : L.marker([pin.latitude, pin.longitude]).addTo(adminBaseLayerGroup).bindPopup(popupContent);
                                adminMarkers[pin.id] = marker;
                            }
                        });

                        // Fit bounds
                        if ((pins || []).length > 1 && adminMap) {
                            var bounds = L.latLngBounds(pins.map(function(p) { return [p.latitude, p.longitude]; }));
                            adminMap.fitBounds(bounds, { padding: [50, 50] });
                        }
                    }).catch(function() { });
                }

                function loadPendingPins() {
                    fetch('/api/admin/map-pins/pending').then(function(r) { return r.ok ? r.json() : []; }).then(function(pins) {
                        var body = document.getElementById('pendingPinsBody');
                        body.innerHTML = '';
                        document.getElementById('pendingCount').textContent = (pins || []).length;
                        showPendingNotice((pins || []).length);
                        if (!pins || !pins.length) {
                            body.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#666;padding:20px;">لا توجد طلبات معلقة حالياً</td></tr>';
                            return;
                        }
                        (pins || []).forEach(function(pin) {
                            var tr = document.createElement('tr');
                            tr.innerHTML =
                                '<td>' + escapeHtml(pin.nameAr || pin.nameEn || '-') + '</td>' +
                                '<td>' + escapeHtml(pin.addressAr || '-') + '</td>' +
                                '<td>' + escapeHtml(pin.phone || '-') + '</td>' +
                                '<td>' + escapeHtml(String(pin.latitude)) + '</td>' +
                                '<td>' + escapeHtml(String(pin.longitude)) + '</td>' +
                                '<td>' +
                                    '<button class="small-btn success" onclick="approvePin(\'' + pin.id + '\')">موافقة</button>' +
                                    '<button class="small-btn danger" onclick="rejectPin(\'' + pin.id + '\')">رفض</button>' +
                                '</td>';
                            body.appendChild(tr);

                            // Add pending marker to admin map
                            if (adminPendingLayerGroup) {
                                var pendingIcon = L.divIcon({
                                    className: 'custom-pending-marker',
                                    html: '<div style="background:#ffb86b;width:14px;height:14px;border-radius:50%;border:3px solid #fff;box-shadow:0 2px 10px rgba(0,0,0,0.5);animation:pulse 1.5s infinite;"></div>',
                                    iconSize: [14, 14],
                                    iconAnchor: [7, 7]
                                });
                                var marker = L.marker([pin.latitude, pin.longitude], { icon: pendingIcon })
                                    .addTo(adminPendingLayerGroup)
                                    .bindPopup('<strong>' + escapeHtml(pin.nameAr || pin.nameEn || '') + '</strong><br>' + escapeHtml(pin.addressAr || '') + '<br>' + escapeHtml(pin.phone || '-') + '<br><em style="color:#ffb86b;">بانتظار الموافقة</em>');
                            }
                        });
                    }).catch(function() { });
                }

                // Visitor-suggested locations waiting for a Manager/Admin decision
                function showPendingNotice(n) {
                    var btn = document.querySelector('.admin-tabs .tab-btn[data-tab="industrial"]');
                    if (btn) {
                        var b = btn.querySelector('.tab-count');
                        if (!b) { b = document.createElement('span'); b.className = 'tab-count'; btn.appendChild(b); }
                        b.textContent = n; b.hidden = !n;
                    }
                    var note = document.getElementById('pinPendingNotice');
                    if (note) {
                        note.hidden = !n;
                        document.getElementById('pinPendingText').textContent = n === 1 ? 'يوجد موقع واحد مقترح من الزوار بانتظار موافقتك' : 'يوجد ' + n + ' مواقع مقترحة من الزوار بانتظار موافقتك';
                    }
                }

                function approvePin(id) {
                    if (!confirm('هل أنت متأكد من الموافقة على هذا الموقع؟ سيظهر في الجدول والخريطة العامة.')) return;
                    fetch('/api/admin/map-pins/' + id + '/approve', { method: 'PUT' })
                    .then(function(r) { return r.json(); })
                    .then(function(res) {
                        if (res.success) {
                            loadMapPins();
                            loadPendingPins();
                        } else {
                            alert('حدث خطأ أثناء الموافقة');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء الموافقة'); });
                }

                function rejectPin(id) {
                    if (!confirm('هل أنت متأكد من رفض وحذف هذا الطلب؟')) return;
                    fetch('/api/admin/map-pins/' + id, { method: 'DELETE' })
                    .then(function(r) { return r.json(); })
                    .then(function(res) {
                        if (res.success) {
                            loadPendingPins();
                        } else {
                            alert('حدث خطأ أثناء الرفض');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء الرفض'); });
                }

                function startEditPin(pin) {
                    editingPinId = pin.id;
                    document.getElementById('pinNameAr').value = pin.nameAr || '';
                    document.getElementById('pinNameEn').value = pin.nameEn || '';
                    document.getElementById('pinNameKu').value = pin.nameKu || '';
                    document.getElementById('pinAddressAr').value = pin.addressAr || '';
                    document.getElementById('pinAddressEn').value = pin.addressEn || '';
                    document.getElementById('pinAddressKu').value = pin.addressKu || '';
                    document.getElementById('pinPhone').value = pin.phone || '';
                    document.getElementById('pinLat').value = pin.latitude;
                    document.getElementById('pinLng').value = pin.longitude;
                    document.getElementById('pinMapEmbedUrl').value = pin.mapEmbedUrl || '';
                    document.getElementById('pinSubmitBtn').textContent = 'حفظ التعديلات';
                    var banner = document.getElementById('pinEditingBanner');
                    banner.classList.add('active');
                    document.getElementById('pinEditingLabel').textContent = 'جاري تعديل: ' + (pin.nameAr || pin.nameEn || '');
                    window.scrollTo({ top: document.getElementById('pinEditingBanner').offsetTop - 100, behavior: 'smooth' });

                    // Pan map to pin
                    if (adminMap && pin.latitude && pin.longitude) {
                        adminMap.flyTo([pin.latitude, pin.longitude], 16, { duration: 1 });
                    }
                }

                function cancelPinEdit() {
                    editingPinId = null;
                    ['pinNameAr','pinNameEn','pinNameKu','pinAddressAr','pinAddressEn','pinAddressKu','pinPhone','pinLat','pinLng','pinMapEmbedUrl'].forEach(function(id) {
                        document.getElementById(id).value = '';
                    });
                    document.getElementById('pinSubmitBtn').textContent = 'إضافة قسم جديد';
                    document.getElementById('pinEditingBanner').classList.remove('active');
                    if (tempPinMarker) {
                        adminMap.removeLayer(tempPinMarker);
                        tempPinMarker = null;
                    }
                }

                function submitMapPin() {
                    var lat = parseFloat(document.getElementById('pinLat').value);
                    var lng = parseFloat(document.getElementById('pinLng').value);
                    if (isNaN(lat) || isNaN(lng)) { alert('الرجاء إدخال خط عرض وخط طول صحيحين'); return; }
                    var payload = {
                        nameAr: document.getElementById('pinNameAr').value,
                        nameEn: document.getElementById('pinNameEn').value,
                        nameKu: document.getElementById('pinNameKu').value,
                        addressAr: document.getElementById('pinAddressAr').value,
                        addressEn: document.getElementById('pinAddressEn').value,
                        addressKu: document.getElementById('pinAddressKu').value,
                        phone: document.getElementById('pinPhone').value,
                        latitude: lat,
                        longitude: lng,
                        mapEmbedUrl: document.getElementById('pinMapEmbedUrl').value
                    };

                    var url = '/api/admin/map-pins';
                    var method = 'POST';
                    if (editingPinId) {
                        url = '/api/admin/map-pins/' + editingPinId;
                        method = 'PUT';
                    }

                    fetch(url, {
                        method: method, headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) {
                            if (tempPinMarker) {
                                adminMap.removeLayer(tempPinMarker);
                                tempPinMarker = null;
                            }
                            cancelPinEdit();
                            loadMapPins();
                        } else {
                            alert('حدث خطأ أثناء حفظ القسم');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء حفظ القسم'); });
                }

                function deleteMapPin(id) {
                    if (!confirm('هل أنت متأكد من حذف هذا القسم؟')) return;
                    fetch('/api/admin/map-pins/' + id, { method: 'DELETE' }).then(function() {
                        if (editingPinId === id) cancelPinEdit();
                        loadMapPins();
                    });
                }

                // ---- Archival album ----
                function loadAlbumItems() {
                    fetch('/api/admin/archival-album').then(function(r) { return r.ok ? r.json() : []; }).then(function(items) {
                        var body = document.getElementById('albumItemsBody');
                        body.innerHTML = '';
                        if (!items || !items.length) {
                            body.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#666;padding:20px;">لا توجد عناصر في الأرشيف</td></tr>';
                            return;
                        }
                        items.forEach(function(item) {
                            var tr = document.createElement('tr');
                            tr.innerHTML =
                                '<td>' + escapeHtml(item.year) + '</td>' +
                                '<td><img src="' + escapeHtml(item.imageUrl) + '" style="width:80px;height:50px;object-fit:cover;border-radius:6px;" onerror="this.style.display=\'none\'" /></td>' +
                                '<td>' + escapeHtml(item.captionAr || '-') + '</td>' +
                                '<td>' + escapeHtml(item.captionEn || '-') + '</td>' +
                                '<td>' + escapeHtml(item.captionKu || '-') + '</td>' +
                                '<td>' +
                                    '<button class="small-btn primary" onclick=\'startEditAlbum(' + JSON.stringify(item) + ')\'>تعديل</button>' +
                                    '<button class="small-btn danger" onclick="deleteAlbumItem(\'' + item.id + '\')">حذف</button>' +
                                '</td>';
                            body.appendChild(tr);
                        });
                    }).catch(function() { });
                }

                function startEditAlbum(item) {
                    editingAlbumId = item.id;
                    document.getElementById('albumYear').value = item.year || '';
                    document.getElementById('albumImageUrl').value = item.imageUrl || '';
                    document.getElementById('albumCaptionAr').value = item.captionAr || '';
                    document.getElementById('albumCaptionEn').value = item.captionEn || '';
                    document.getElementById('albumCaptionKu').value = item.captionKu || '';
                    document.getElementById('albumSubmitBtn').textContent = 'حفظ التعديلات';
                    var banner = document.getElementById('albumEditingBanner');
                    banner.classList.add('active');
                    document.getElementById('albumEditingLabel').textContent = 'جاري تعديل: ' + (item.year || '');
                    window.scrollTo({ top: document.getElementById('albumEditingBanner').offsetTop - 100, behavior: 'smooth' });
                }

                function cancelAlbumEdit() {
                    editingAlbumId = null;
                    ['albumYear','albumImageUrl','albumCaptionAr','albumCaptionEn','albumCaptionKu'].forEach(function(id) {
                        document.getElementById(id).value = '';
                    });
                    document.getElementById('albumSubmitBtn').textContent = 'إضافة عنصر جديد';
                    document.getElementById('albumEditingBanner').classList.remove('active');
                }

                function submitAlbumItem() {
                    var year = document.getElementById('albumYear').value.trim();
                    var imageUrl = document.getElementById('albumImageUrl').value.trim();
                    if (!year || !imageUrl) { alert('الرجاء إدخال السنة ورابط الصورة على الأقل'); return; }
                    var payload = {
                        year: year,
                        imageUrl: imageUrl,
                        captionAr: document.getElementById('albumCaptionAr').value,
                        captionEn: document.getElementById('albumCaptionEn').value,
                        captionKu: document.getElementById('albumCaptionKu').value
                    };
                    var url = '/api/admin/archival-album';
                    var method = 'POST';
                    if (editingAlbumId) {
                        url = '/api/admin/archival-album/' + editingAlbumId;
                        method = 'PUT';
                    }
                    fetch(url, {
                        method: method, headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) {
                            cancelAlbumEdit();
                            loadAlbumItems();
                        } else {
                            alert('حدث خطأ أثناء حفظ العنصر');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء حفظ العنصر'); });
                }

                function deleteAlbumItem(id) {
                    if (!confirm('هل أنت متأكد من حذف هذا العنصر من الأرشيف؟')) return;
                    fetch('/api/admin/archival-album/' + id, { method: 'DELETE' }).then(function() {
                        if (editingAlbumId === id) cancelAlbumEdit();
                        loadAlbumItems();
                    });
                }

                function uploadAlbumImage() {
                    var input = document.getElementById('albumImageUploadInput');
                    if (!input.files || !input.files[0]) { alert('اختر صورة أولاً'); return; }
                    var formData = new FormData();
                    formData.append('file', input.files[0]);
                    document.getElementById('albumUploadResult').textContent = 'جاري الرفع...';
                    fetch('/api/admin/upload-image', { method: 'POST', body: formData })
                    .then(function(r) { return r.json().then(function(data) { return { ok: r.ok, data: data }; }); })
                    .then(function(res) {
                        if (res.ok) {
                            document.getElementById('albumUploadResult').textContent = 'تم الرفع: ' + res.data.url;
                            document.getElementById('albumImageUrl').value = res.data.url;
                        } else {
                            document.getElementById('albumUploadResult').textContent = res.data.message || 'فشل الرفع';
                        }
                    }).catch(function() { document.getElementById('albumUploadResult').textContent = 'فشل الرفع'; });
                }

                // ---- Explorer cards (admin) ----
                function loadExplorerCardsAdmin() {
                    fetch('/api/admin/explorer-cards').then(function(r) { return r.ok ? r.json() : []; }).then(function(cards) {
                        var body = document.getElementById('explorerCardsBody');
                        body.innerHTML = '';
                        if (!cards || !cards.length) {
                            body.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#666;padding:20px;">لا توجد بطاقات</td></tr>';
                            return;
                        }
                        cards.forEach(function(c) {
                            var tr = document.createElement('tr');
                            tr.innerHTML =
                                '<td style="font-size:1.4rem;text-align:center;">' + escapeHtml(c.icon || '-') + '</td>' +
                                '<td><img src="' + escapeHtml(c.imageUrl) + '" style="width:80px;height:50px;object-fit:cover;border-radius:6px;" onerror="this.style.display=\'none\'" /></td>' +
                                '<td>' + escapeHtml(c.titleAr || '-') + '</td>' +
                                '<td style="max-width:200px;white-space:normal;">' + escapeHtml(c.tags || '-') + '</td>' +
                                '<td>' + escapeHtml(String(c.sortOrder || 0)) + '</td>' +
                                '<td>' +
                                    '<button class="small-btn primary" onclick=\'startEditExplorer(' + JSON.stringify(c).replace(/'/g, "\\'") + ')\'>تعديل</button>' +
                                    '<button class="small-btn danger" onclick="deleteExplorerCard(\'' + c.id + '\')">حذف</button>' +
                                '</td>';
                            body.appendChild(tr);
                        });
                    }).catch(function() { });
                }

                function startEditExplorer(card) {
                    editingExplorerId = card.id;
                    document.getElementById('explorerIcon').value = card.icon || '';
                    document.getElementById('explorerImageUrl').value = card.imageUrl || '';
                    document.getElementById('explorerTitleAr').value = card.titleAr || '';
                    document.getElementById('explorerTitleEn').value = card.titleEn || '';
                    document.getElementById('explorerTitleKu').value = card.titleKu || '';
                    document.getElementById('explorerDescAr').value = card.descAr || '';
                    document.getElementById('explorerDescEn').value = card.descEn || '';
                    document.getElementById('explorerDescKu').value = card.descKu || '';
                    document.getElementById('explorerTags').value = card.tags || '';
                    document.getElementById('explorerLink').value = card.link || '/guide';
                    document.getElementById('explorerSortOrder').value = card.sortOrder || 0;
                    document.getElementById('explorerSubmitBtn').textContent = 'حفظ التعديلات';
                    var banner = document.getElementById('explorerEditingBanner');
                    banner.classList.add('active');
                    document.getElementById('explorerEditingLabel').textContent = 'جاري تعديل: ' + (card.titleAr || '');
                    window.scrollTo({ top: document.getElementById('explorerEditingBanner').offsetTop - 100, behavior: 'smooth' });
                }

                function cancelExplorerEdit() {
                    editingExplorerId = null;
                    ['explorerIcon','explorerImageUrl','explorerTitleAr','explorerTitleEn','explorerTitleKu','explorerDescAr','explorerDescEn','explorerDescKu','explorerTags','explorerLink','explorerSortOrder'].forEach(function(id) {
                        document.getElementById(id).value = '';
                    });
                    document.getElementById('explorerIcon').value = '';
                    document.getElementById('explorerLink').value = '/guide';
                    document.getElementById('explorerSortOrder').value = '0';
                    document.getElementById('explorerSubmitBtn').textContent = 'إضافة بطاقة جديدة';
                    document.getElementById('explorerEditingBanner').classList.remove('active');
                }

                function submitExplorerCard() {
                    var payload = {
                        icon: document.getElementById('explorerIcon').value || '',
                        imageUrl: document.getElementById('explorerImageUrl').value,
                        titleAr: document.getElementById('explorerTitleAr').value,
                        titleEn: document.getElementById('explorerTitleEn').value,
                        titleKu: document.getElementById('explorerTitleKu').value,
                        descAr: document.getElementById('explorerDescAr').value,
                        descEn: document.getElementById('explorerDescEn').value,
                        descKu: document.getElementById('explorerDescKu').value,
                        tags: document.getElementById('explorerTags').value,
                        link: document.getElementById('explorerLink').value || '/guide',
                        sortOrder: parseInt(document.getElementById('explorerSortOrder').value) || 0
                    };
                    var url = '/api/admin/explorer-cards';
                    var method = 'POST';
                    if (editingExplorerId) {
                        url = '/api/admin/explorer-cards/' + editingExplorerId;
                        method = 'PUT';
                    }
                    fetch(url, {
                        method: method, headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) {
                            cancelExplorerEdit();
                            loadExplorerCardsAdmin();
                        } else {
                            alert('حدث خطأ أثناء حفظ البطاقة');
                        }
                    }).catch(function() { alert('حدث خطأ أثناء حفظ البطاقة'); });
                }

                function deleteExplorerCard(id) {
                    if (!confirm('هل أنت متأكد من حذف هذه البطاقة؟')) return;
                    fetch('/api/admin/explorer-cards/' + id, { method: 'DELETE' }).then(function() {
                        if (editingExplorerId === id) cancelExplorerEdit();
                        loadExplorerCardsAdmin();
                    });
                }

                function uploadExplorerImage() {
                    var input = document.getElementById('explorerImageUploadInput');
                    if (!input.files || !input.files[0]) { alert('اختر صورة أولاً'); return; }
                    var formData = new FormData();
                    formData.append('file', input.files[0]);
                    document.getElementById('explorerUploadResult').textContent = 'جاري الرفع...';
                    fetch('/api/admin/upload-image', { method: 'POST', body: formData })
                    .then(function(r) { return r.json().then(function(data) { return { ok: r.ok, data: data }; }); })
                    .then(function(res) {
                        if (res.ok) {
                            document.getElementById('explorerUploadResult').textContent = 'تم الرفع: ' + res.data.url;
                            document.getElementById('explorerImageUrl').value = res.data.url;
                        } else {
                            document.getElementById('explorerUploadResult').textContent = res.data.message || 'فشل الرفع';
                        }
                    }).catch(function() { document.getElementById('explorerUploadResult').textContent = 'فشل الرفع'; });
                }

                // ---- Guide content (admin) ----
                var guideAdminData = { translations: { ar: {}, en: {}, ku: {} }, topics: [], equipment: [] };

                function loadGuideContentAdmin() {
                    fetch('/api/admin/guide-content').then(function(r) { return r.ok ? r.json() : null; }).then(function(data) {
                        if (data) {
                            guideAdminData = data;
                            if (!guideAdminData.topics) guideAdminData.topics = [];
                            if (!guideAdminData.equipment) guideAdminData.equipment = [];
                        }
                        renderGuideTopicsAdmin();
                        renderGuideEquipAdmin();
                    }).catch(function() { });
                }

                function renderGuideTopicsAdmin() {
                    var body = document.getElementById('guideTopicsBody');
                    body.innerHTML = '';
                    if (!guideAdminData.topics.length) {
                        body.innerHTML = '<tr><td colspan="4" style="text-align:center;color:#666;padding:20px;">لا توجد مواضيع</td></tr>';
                        return;
                    }
                    guideAdminData.topics.forEach(function(t) {
                        var tr = document.createElement('tr');
                        tr.innerHTML =
                            '<td style="font-size:1.3rem;text-align:center;">' + escapeHtml(t.icon || '-') + '</td>' +
                            '<td>' + escapeHtml(t.titleAr || '-') + '</td>' +
                            '<td>' + escapeHtml(String(t.sortOrder || 0)) + '</td>' +
                            '<td>' +
                                '<button class="small-btn primary" onclick=\'startEditGuideTopic(' + JSON.stringify(t).replace(/'/g, "\\'") + ')\'>تعديل</button>' +
                                '<button class="small-btn danger" onclick="deleteGuideTopic(\'' + t.id + '\')">حذف</button>' +
                            '</td>';
                        body.appendChild(tr);
                    });
                }

                function startEditGuideTopic(t) {
                    editingGuideTopicId = t.id;
                    document.getElementById('guideTopicIcon').value = t.icon || '';
                    document.getElementById('guideTopicSortOrder').value = t.sortOrder || 0;
                    document.getElementById('guideTopicTitleAr').value = t.titleAr || '';
                    document.getElementById('guideTopicTitleEn').value = t.titleEn || '';
                    document.getElementById('guideTopicTitleKu').value = t.titleKu || '';
                    document.getElementById('guideTopicBodyAr').value = t.bodyAr || '';
                    document.getElementById('guideTopicBodyEn').value = t.bodyEn || '';
                    document.getElementById('guideTopicBodyKu').value = t.bodyKu || '';
                    document.getElementById('guideTopicSubmitBtn').textContent = 'حفظ التعديلات';
                    document.getElementById('guideTopicEditingBanner').classList.add('active');
                    document.getElementById('guideTopicEditingLabel').textContent = 'جاري تعديل: ' + (t.titleAr || '');
                }

                function cancelGuideTopicEdit() {
                    editingGuideTopicId = null;
                    ['guideTopicIcon','guideTopicSortOrder','guideTopicTitleAr','guideTopicTitleEn','guideTopicTitleKu','guideTopicBodyAr','guideTopicBodyEn','guideTopicBodyKu'].forEach(function(id) {
                        document.getElementById(id).value = '';
                    });
                    document.getElementById('guideTopicIcon').value = '';
                    document.getElementById('guideTopicSortOrder').value = '0';
                    document.getElementById('guideTopicSubmitBtn').textContent = 'إضافة موضوع جديد';
                    document.getElementById('guideTopicEditingBanner').classList.remove('active');
                }

                function submitGuideTopic() {
                    var payload = {
                        icon: document.getElementById('guideTopicIcon').value || '',
                        sortOrder: parseInt(document.getElementById('guideTopicSortOrder').value) || 0,
                        titleAr: document.getElementById('guideTopicTitleAr').value,
                        titleEn: document.getElementById('guideTopicTitleEn').value,
                        titleKu: document.getElementById('guideTopicTitleKu').value,
                        bodyAr: document.getElementById('guideTopicBodyAr').value,
                        bodyEn: document.getElementById('guideTopicBodyEn').value,
                        bodyKu: document.getElementById('guideTopicBodyKu').value
                    };
                    if (editingGuideTopicId) {
                        payload.id = editingGuideTopicId;
                        // update existing
                        for (var i = 0; i < guideAdminData.topics.length; i++) {
                            if (guideAdminData.topics[i].id === editingGuideTopicId) {
                                guideAdminData.topics[i] = payload;
                                break;
                            }
                        }
                    } else {
                        payload.id = 'new_' + Date.now();
                        guideAdminData.topics.push(payload);
                    }
                    fetch('/api/admin/guide-content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(guideAdminData)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) { cancelGuideTopicEdit(); loadGuideContentAdmin(); }
                        else { alert('حدث خطأ أثناء الحفظ'); }
                    }).catch(function() { alert('حدث خطأ أثناء الحفظ'); });
                }

                function deleteGuideTopic(id) {
                    if (!confirm('هل أنت متأكد من حذف هذا الموضوع؟')) return;
                    guideAdminData.topics = guideAdminData.topics.filter(function(t) { return t.id !== id; });
                    fetch('/api/admin/guide-content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(guideAdminData)
                    }).then(function(r) { return r.json(); }).then(function() { loadGuideContentAdmin(); });
                }

                function renderGuideEquipAdmin() {
                    var body = document.getElementById('guideEquipBody');
                    body.innerHTML = '';
                    if (!guideAdminData.equipment.length) {
                        body.innerHTML = '<tr><td colspan="4" style="text-align:center;color:#666;padding:20px;">لا توجد معدات</td></tr>';
                        return;
                    }
                    guideAdminData.equipment.forEach(function(eq) {
                        var tr = document.createElement('tr');
                        tr.innerHTML =
                            '<td>' + escapeHtml(eq.nameAr || '-') + '</td>' +
                            '<td>' + escapeHtml(eq.specAr || '-') + '</td>' +
                            '<td>' + escapeHtml(String(eq.sortOrder || 0)) + '</td>' +
                            '<td>' +
                                '<button class="small-btn primary" onclick=\'startEditGuideEquip(' + JSON.stringify(eq).replace(/'/g, "\\'") + ')\'>تعديل</button>' +
                                '<button class="small-btn danger" onclick="deleteGuideEquip(\'' + eq.id + '\')">حذف</button>' +
                            '</td>';
                        body.appendChild(tr);
                    });
                }

                function startEditGuideEquip(eq) {
                    editingGuideEquipId = eq.id;
                    document.getElementById('guideEquipNameAr').value = eq.nameAr || '';
                    document.getElementById('guideEquipNameEn').value = eq.nameEn || '';
                    document.getElementById('guideEquipNameKu').value = eq.nameKu || '';
                    document.getElementById('guideEquipSpecAr').value = eq.specAr || '';
                    document.getElementById('guideEquipSpecEn').value = eq.specEn || '';
                    document.getElementById('guideEquipSpecKu').value = eq.specKu || '';
                    document.getElementById('guideEquipUseAr').value = eq.useAr || '';
                    document.getElementById('guideEquipUseEn').value = eq.useEn || '';
                    document.getElementById('guideEquipUseKu').value = eq.useKu || '';
                    document.getElementById('guideEquipSortOrder').value = eq.sortOrder || 0;
                    document.getElementById('guideEquipSubmitBtn').textContent = 'حفظ التعديلات';
                    document.getElementById('guideEquipEditingBanner').classList.add('active');
                    document.getElementById('guideEquipEditingLabel').textContent = 'جاري تعديل: ' + (eq.nameAr || '');
                }

                function cancelGuideEquipEdit() {
                    editingGuideEquipId = null;
                    ['guideEquipNameAr','guideEquipNameEn','guideEquipNameKu','guideEquipSpecAr','guideEquipSpecEn','guideEquipSpecKu','guideEquipUseAr','guideEquipUseEn','guideEquipUseKu','guideEquipSortOrder'].forEach(function(id) {
                        document.getElementById(id).value = '';
                    });
                    document.getElementById('guideEquipSortOrder').value = '0';
                    document.getElementById('guideEquipSubmitBtn').textContent = 'إضافة معدة جديدة';
                    document.getElementById('guideEquipEditingBanner').classList.remove('active');
                }

                function submitGuideEquip() {
                    var payload = {
                        nameAr: document.getElementById('guideEquipNameAr').value,
                        nameEn: document.getElementById('guideEquipNameEn').value,
                        nameKu: document.getElementById('guideEquipNameKu').value,
                        specAr: document.getElementById('guideEquipSpecAr').value,
                        specEn: document.getElementById('guideEquipSpecEn').value,
                        specKu: document.getElementById('guideEquipSpecKu').value,
                        useAr: document.getElementById('guideEquipUseAr').value,
                        useEn: document.getElementById('guideEquipUseEn').value,
                        useKu: document.getElementById('guideEquipUseKu').value,
                        sortOrder: parseInt(document.getElementById('guideEquipSortOrder').value) || 0
                    };
                    if (editingGuideEquipId) {
                        payload.id = editingGuideEquipId;
                        for (var i = 0; i < guideAdminData.equipment.length; i++) {
                            if (guideAdminData.equipment[i].id === editingGuideEquipId) {
                                guideAdminData.equipment[i] = payload;
                                break;
                            }
                        }
                    } else {
                        payload.id = 'new_' + Date.now();
                        guideAdminData.equipment.push(payload);
                    }
                    fetch('/api/admin/guide-content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(guideAdminData)
                    }).then(function(r) { return r.json(); }).then(function(res) {
                        if (res.success) { cancelGuideEquipEdit(); loadGuideContentAdmin(); }
                        else { alert('حدث خطأ أثناء الحفظ'); }
                    }).catch(function() { alert('حدث خطأ أثناء الحفظ'); });
                }

                function deleteGuideEquip(id) {
                    if (!confirm('هل أنت متأكد من حذف هذه المعدة؟')) return;
                    guideAdminData.equipment = guideAdminData.equipment.filter(function(e) { return e.id !== id; });
                    fetch('/api/admin/guide-content', {
                        method: 'PUT', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(guideAdminData)
                    }).then(function(r) { return r.json(); }).then(function() { loadGuideContentAdmin(); });
                }

                // ---- Image upload (hero) ----
                function uploadImage() {
                    var input = document.getElementById('imageUploadInput');
                    if (!input.files || !input.files[0]) { alert('اختر صورة أولاً'); return; }
                    var formData = new FormData();
                    formData.append('file', input.files[0]);
                    document.getElementById('uploadResult').textContent = 'جاري الرفع...';
                    fetch('/api/admin/upload-image', { method: 'POST', body: formData })
                    .then(function(r) { return r.json().then(function(data) { return { ok: r.ok, data: data }; }); })
                    .then(function(res) {
                        if (res.ok) {
                            document.getElementById('uploadResult').textContent = 'تم الرفع: ' + res.data.url + ' (انسخ الرابط وألصقه في حقل الصورة المطلوب)';
                        } else {
                            document.getElementById('uploadResult').textContent = res.data.message || 'فشل الرفع';
                        }
                    }).catch(function() { document.getElementById('uploadResult').textContent = 'فشل الرفع'; });
                }

                function escapeHtml(str) {
                    if (str === null || str === undefined) return '';
                    return String(str)
                        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                        .replace(/"/g, '&quot;').replace(/'/g, '&#039;');
                }

                // =========================================================
                // ======================= GALLERY =========================
                // =========================================================
                var galleryState = { max: 60, items: [], maxImageMb: 12, maxVideoMb: 60 };

                function loadGalleryAdmin() {
                    return fetch('/api/admin/gallery').then(function(r) { return r.ok ? r.json() : null; }).then(function(data) {
                        if (!data) return;
                        galleryState = data;
                        renderGalleryAdmin();
                    }).catch(function() { });
                }

                function renderGalleryAdmin() {
                    var items = galleryState.items || [];
                    var max = galleryState.max || 60;
                    document.getElementById('galCount').textContent = items.length + ' / ' + max;
                    document.getElementById('galMeterFill').style.width = Math.min(100, items.length / max * 100) + '%';
                    var full = items.length >= max;
                    document.getElementById('galMeter').classList.toggle('full', full);
                    document.getElementById('galUploadBtn').disabled = full;
                    document.getElementById('galFiles').disabled = full;
                    document.getElementById('galLimits').textContent = full
                        ? 'المعرض ممتلئ (' + max + ' ملفاً). احذف ملفاً لإضافة جديد.'
                        : 'الصور حتى ' + galleryState.maxImageMb + 'MB — الفيديو حتى ' + galleryState.maxVideoMb + 'MB (MP4 / MOV / WEBM) — المتبقي: ' + (max - items.length) + ' من ' + max;

                    var grid = document.getElementById('galGrid');
                    grid.innerHTML = '';
                    if (!items.length) {
                        grid.innerHTML = '<p style="color:#777;padding:16px;">لا توجد ملفات في المعرض بعد.</p>';
                        return;
                    }
                    items.forEach(function(it, i) {
                        var card = document.createElement('div');
                        card.className = 'gal-card';
                        var media = it.type === 'video'
                            ? '<video src="' + escapeHtml(it.url) + '#t=0.1" preload="metadata" muted playsinline controls></video>'
                            : '<img src="' + escapeHtml(it.url) + '" loading="lazy" alt="" />';
                        card.innerHTML =
                            '<div class="gal-thumb">' + media +
                                '<span class="gal-badge">' + (it.type === 'video' ? 'فيديو' : 'صورة') + ' · ' + ((it.size || 0) < 1048576 ? Math.max(1, Math.round((it.size || 0) / 1024)) + 'KB' : (Math.round(it.size / 1048576 * 10) / 10) + 'MB') + '</span>' +
                                '<span class="gal-pos">#' + (i + 1) + '</span>' +
                            '</div>' +
                            '<div class="gal-body">' +
                                '<input type="text" data-k="captionAr" maxlength="300" placeholder="الوصف بالعربية" value="' + escapeHtml(it.captionAr || '') + '" />' +
                                '<input type="text" data-k="captionEn" maxlength="300" placeholder="English caption" value="' + escapeHtml(it.captionEn || '') + '" />' +
                                '<input type="text" data-k="captionKu" maxlength="300" placeholder="ناونیشانی کوردی" value="' + escapeHtml(it.captionKu || '') + '" />' +
                            '</div>' +
                            '<div class="gal-actions">' +
                                '<button class="small-btn primary" data-act="save">حفظ الوصف</button>' +
                                '<button class="small-btn" data-act="up" title="تقديم" ' + (i === 0 ? 'disabled' : '') + '>▲</button>' +
                                '<button class="small-btn" data-act="down" title="تأخير" ' + (i === items.length - 1 ? 'disabled' : '') + '>▼</button>' +
                                '<button class="small-btn danger" data-act="del">حذف</button>' +
                            '</div>';
                        card.querySelector('[data-act="save"]').onclick = function(e) {
                            var payload = {};
                            card.querySelectorAll('input[data-k]').forEach(function(inp) { payload[inp.getAttribute('data-k')] = inp.value; });
                            var btn = e.currentTarget;
                            fetch('/api/admin/gallery/' + it.id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
                                .then(function(r) { btn.textContent = r.ok ? 'تم الحفظ' : 'فشل الحفظ'; setTimeout(function() { btn.textContent = 'حفظ الوصف'; }, 1800); if (r.ok) { it.captionAr = payload.captionAr; it.captionEn = payload.captionEn; it.captionKu = payload.captionKu; } });
                        };
                        card.querySelector('[data-act="up"]').onclick = function() { moveGalleryItem(it.id, 'up'); };
                        card.querySelector('[data-act="down"]').onclick = function() { moveGalleryItem(it.id, 'down'); };
                        card.querySelector('[data-act="del"]').onclick = function() {
                            if (!confirm('حذف هذا الملف من المعرض نهائياً؟')) return;
                            fetch('/api/admin/gallery/' + it.id, { method: 'DELETE' }).then(function() { loadGalleryAdmin(); });
                        };
                        grid.appendChild(card);
                    });
                }

                function moveGalleryItem(id, dir) {
                    fetch('/api/admin/gallery/' + id + '/move', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dir: dir }) })
                        .then(function() { loadGalleryAdmin(); });
                }

                // Sends one file in 4 MB pieces (each piece retried up to 3 times), so large videos
                // on slow or unstable connections still arrive.
                function sendGalleryChunk(fd) {
                    return new Promise(function(resolve) {
                        var xhr = new XMLHttpRequest();
                        xhr.open('POST', '/api/admin/gallery/chunk');
                        xhr.timeout = 240000;
                        xhr.onload = function() {
                            var data = {};
                            try { data = JSON.parse(xhr.responseText); } catch (e) { }
                            resolve({ status: xhr.status, data: data });
                        };
                        xhr.onerror = function() { resolve({ status: 0, data: {} }); };
                        xhr.ontimeout = function() { resolve({ status: 0, data: {} }); };
                        xhr.send(fd);
                    });
                }

                async function uploadOneGalleryFile(file, row) {
                    var CHUNK = 4 * 1024 * 1024;
                    var total = Math.max(1, Math.ceil(file.size / CHUNK));
                    var uploadId = '';
                    var hex = '0123456789abcdef';
                    for (var k = 0; k < 32; k++) uploadId += hex[Math.floor(Math.random() * 16)];
                    var status = row.lastChild;
                    for (var i = 0; i < total; i++) {
                        var fd = new FormData();
                        fd.append('uploadId', uploadId);
                        fd.append('index', String(i));
                        fd.append('total', String(total));
                        fd.append('fileName', file.name);
                        fd.append('size', String(file.size));
                        if (i === total - 1) {
                            fd.append('captionAr', document.getElementById('galCapAr').value);
                            fd.append('captionEn', document.getElementById('galCapEn').value);
                            fd.append('captionKu', document.getElementById('galCapKu').value);
                        }
                        fd.append('chunk', file.slice(i * CHUNK, Math.min(file.size, (i + 1) * CHUNK)), 'chunk');
                        var res = null;
                        for (var attempt = 1; attempt <= 3; attempt++) {
                            res = await sendGalleryChunk(fd);
                            if (res.status !== 0 && res.status < 500) break;
                            status.textContent = 'إعادة المحاولة (' + attempt + ')…';
                            await new Promise(function(r) { setTimeout(r, 2000 * attempt); });
                        }
                        if (!(res.status >= 200 && res.status < 300 && res.data.success)) {
                            status.textContent = res.data.message || (res.status ? 'فشل (' + res.status + ')' : 'انقطع الاتصال، أعد المحاولة');
                            status.className = 'err';
                            return res.status === 409 ? 'full' : false;
                        }
                        status.textContent = Math.round((i + 1) / total * 100) + '%';
                    }
                    status.textContent = 'تم'; status.className = 'ok';
                    return true;
                }

                async function uploadGalleryFiles() {
                    var input = document.getElementById('galFiles');
                    var files = Array.prototype.slice.call(input.files || []);
                    if (!files.length) { alert('اختر ملفاً واحداً على الأقل'); return; }
                    var room = (galleryState.max || 60) - (galleryState.items || []).length;
                    var progress = document.getElementById('galProgress');
                    progress.innerHTML = '';
                    var btn = document.getElementById('galUploadBtn');
                    btn.disabled = true;
                    for (var i = 0; i < files.length; i++) {
                        var f = files[i];
                        var row = document.createElement('div');
                        row.innerHTML = '<span></span><span class="run">…</span>';
                        row.firstChild.textContent = f.name;
                        progress.appendChild(row);
                        if (i >= room) { row.lastChild.textContent = 'تم تجاوز الحد الأقصى (' + galleryState.max + ')'; row.lastChild.className = 'err'; continue; }
                        var isVideo = /^video\//.test(f.type) || /\.(mov|mp4|m4v|webm)$/i.test(f.name);
                        var limit = (isVideo ? galleryState.maxVideoMb : galleryState.maxImageMb) * 1048576;
                        if (f.size > limit) { row.lastChild.textContent = 'الحجم أكبر من ' + (limit / 1048576) + 'MB'; row.lastChild.className = 'err'; continue; }
                        var res = await uploadOneGalleryFile(f, row);
                        if (res === 'full') room = i; // server says full: skip the rest
                    }
                    input.value = '';
                    await loadGalleryAdmin();
                    btn.disabled = (galleryState.items || []).length >= (galleryState.max || 60);
                }

                (function() {
                    var drop = document.getElementById('galDrop');
                    var input = document.getElementById('galFiles');
                    if (!drop || !input) return;
                    ['dragenter', 'dragover'].forEach(function(ev) { drop.addEventListener(ev, function(e) { e.preventDefault(); drop.classList.add('drag'); }); });
                    ['dragleave', 'drop'].forEach(function(ev) { drop.addEventListener(ev, function(e) { e.preventDefault(); drop.classList.remove('drag'); }); });
                    drop.addEventListener('drop', function(e) { if (e.dataTransfer && e.dataTransfer.files.length) { input.files = e.dataTransfer.files; } });
                })();

                // =========================================================
                // ================= SUBSCRIPTION / BILLING ================
                // =========================================================
                var billingCanEdit = false;
                var BILL_STATUS = { paid: 'مدفوعة', partial: 'مدفوعة جزئياً', due: 'مستحقة', overdue: 'متأخرة', upcoming: 'لم تبدأ بعد' };

                function money(v) {
                    var n = Number(v || 0);
                    // LTR isolate so "$20.00" reads correctly inside Arabic sentences
                    return '\u2066$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '\u2069';
                }
                function niceDate(s) {
                    if (!s) return '-';
                    var p = s.split('-');
                    var d = new Date(Date.UTC(+p[0], +p[1] - 1, +p[2]));
                    try { return d.toLocaleDateString('ar-u-nu-latn', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' }); }
                    catch (e) { return s; }
                }

                // "!" notes: fill each with the explanation; hover/focus shows it, a tap toggles it on phones
                (function() {
                    var tpl = document.getElementById('billInfoTpl');
                    document.querySelectorAll('.bill-info').forEach(function(b) {
                        if (tpl && !b.querySelector('.bill-pop')) b.appendChild(tpl.content.firstElementChild.cloneNode(true));
                    });
                    document.addEventListener('click', function(e) {
                        document.querySelectorAll('.bill-info.open').forEach(function(b) { if (!b.contains(e.target)) b.classList.remove('open'); });
                    });
                })();
                function toggleBillInfo(e, btn) {
                    e.stopPropagation();
                    if (e.target.closest && e.target.closest('.bill-pop')) return;
                    btn.classList.toggle('open');
                }

                function loadBilling() {
                    return fetch('/api/admin/billing').then(function(r) { return r.ok ? r.json() : null; }).then(function(data) {
                        if (!data) return;
                        billingCanEdit = !!data.canEdit;
                        renderBilling(data.summary);
                    }).catch(function() { });
                }

                function renderBilling(s) {
                    var st = s.status;
                    // header strip
                    var strip = document.getElementById('billStrip');
                    strip.hidden = false;
                    strip.className = 'bill-strip s-' + st;
                    var stripText = st === 'paid' ? 'مدفوع — لا يوجد مبلغ مستحق'
                        : st === 'overdue' ? 'متأخر — المستحق ' + money(s.balance)
                        : st === 'upcoming' ? 'يبدأ في ' + niceDate(s.startDate)
                        : 'مستحق ' + money(s.balance);
                    document.getElementById('billStripStatus').textContent = stripText;
                    document.getElementById('billStripMeta').textContent = st === 'paid'
                        ? 'الاستحقاق القادم: ' + niceDate(s.nextRenewal) + ' — ' + money(s.monthlyAmount) + ' شهرياً'
                        : 'تاريخ الاستحقاق: ' + niceDate(s.dueDate) + ' — الاستحقاق القادم: ' + niceDate(s.nextRenewal);

                    // invoice
                    var badge = document.getElementById('invStatus');
                    badge.className = 'invoice-status s-' + st;
                    badge.textContent = st === 'paid' ? 'مدفوعة' : (BILL_STATUS[st] || '');
                    document.getElementById('invNumber').textContent = s.currentInvoice || '-';
                    document.getElementById('invPeriod').textContent = niceDate(s.currentPeriodStart) + ' – ' + niceDate(s.currentPeriodEnd);
                    document.getElementById('invDue').textContent = niceDate(s.dueDate);
                    document.getElementById('invNext').textContent = niceDate(s.nextRenewal);
                    document.getElementById('invMonthly').textContent = money(s.monthlyAmount);
                    document.getElementById('invPrev').textContent = money(s.previousBalance);
                    document.getElementById('invTotal').textContent = money(s.balance);
                    document.getElementById('invCreditRow').hidden = !(s.credit > 0);
                    document.getElementById('invCredit').textContent = '-' + money(s.credit);
                    document.getElementById('invDay').textContent = String(+s.startDate.split('-')[2]);

                    document.getElementById('sumBilled').textContent = money(s.totalBilled);
                    document.getElementById('sumPaid').textContent = money(s.totalPaid);
                    document.getElementById('sumBalance').textContent = money(s.balance);

                    // payment form (Admin only)
                    var form = document.getElementById('payFormCard');
                    form.hidden = !billingCanEdit;
                    if (billingCanEdit) {
                        var amt = document.getElementById('payAmount');
                        if (!amt.dataset.touched) amt.value = s.balance > 0 ? Number(s.balance).toFixed(2) : Number(s.monthlyAmount).toFixed(2);
                        var dt = document.getElementById('payDate');
                        if (!dt.value) dt.value = s.today;
                        dt.max = s.today;
                    }
                    document.querySelectorAll('.admin-only-col').forEach(function(th) { th.style.display = billingCanEdit ? '' : 'none'; });

                    // payments
                    var pb = document.getElementById('payRows');
                    pb.innerHTML = '';
                    if (!s.payments || !s.payments.length) {
                        pb.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#666;padding:18px;">لا توجد دفعات مسجلة</td></tr>';
                    } else {
                        s.payments.forEach(function(p) {
                            var tr = document.createElement('tr');
                            tr.innerHTML = '<td>' + escapeHtml(niceDate(p.paidOn)) + '</td>' +
                                '<td class="num">' + money(p.amount) + '</td>' +
                                '<td>' + escapeHtml(p.note || '-') + '</td>' +
                                '<td style="direction:ltr;text-align:start;">' + escapeHtml(p.recordedBy || '-') + '</td>' +
                                (billingCanEdit ? '<td><button class="small-btn danger" data-id="' + escapeHtml(p.id) + '">حذف</button></td>' : '');
                            var del = tr.querySelector('button[data-id]');
                            if (del) del.onclick = function() { deletePayment(p.id, p.amount); };
                            pb.appendChild(tr);
                        });
                    }

                    // invoices
                    var ib = document.getElementById('invRows');
                    ib.innerHTML = '';
                    (s.invoices || []).forEach(function(inv) {
                        var tr = document.createElement('tr');
                        tr.innerHTML = '<td style="direction:ltr;text-align:start;">' + escapeHtml(inv.number) + '</td>' +
                            '<td>' + escapeHtml(niceDate(inv.periodStart)) + ' – ' + escapeHtml(niceDate(inv.periodEnd)) + '</td>' +
                            '<td class="num">' + money(inv.amount) + '</td>' +
                            '<td class="num">' + money(inv.paid) + '</td>' +
                            '<td class="num">' + money(inv.remaining) + '</td>' +
                            '<td><span class="bill-pill s-' + inv.status + '">' + (BILL_STATUS[inv.status] || inv.status) + '</span></td>';
                        ib.appendChild(tr);
                    });
                }

                function recordPayment() {
                    var amount = document.getElementById('payAmount').value.trim();
                    var paidOn = document.getElementById('payDate').value;
                    var note = document.getElementById('payNote').value;
                    var msg = document.getElementById('payMsg');
                    msg.className = 'bill-msg'; msg.textContent = '';
                    if (!amount || Number(amount) <= 0) { msg.className = 'bill-msg err'; msg.textContent = 'أدخل مبلغاً صحيحاً'; return; }
                    if (!confirm('تسجيل دفعة بمبلغ ' + money(amount) + ' بتاريخ ' + niceDate(paidOn) + '؟')) return;
                    var btn = document.getElementById('payBtn');
                    btn.disabled = true;
                    fetch('/api/admin/billing/payments', {
                        method: 'POST', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ amount: String(amount), paidOn: paidOn, note: note })
                    }).then(function(r) { return r.json().then(function(d) { return { ok: r.ok, d: d }; }); })
                    .then(function(res) {
                        btn.disabled = false;
                        if (res.ok && res.d.success) {
                            msg.className = 'bill-msg ok'; msg.textContent = 'تم تسجيل الدفعة';
                            document.getElementById('payNote').value = '';
                            delete document.getElementById('payAmount').dataset.touched;
                            renderBilling(res.d.summary);
                        } else {
                            msg.className = 'bill-msg err'; msg.textContent = res.d.message || 'تعذر تسجيل الدفعة';
                        }
                    }).catch(function() { btn.disabled = false; msg.className = 'bill-msg err'; msg.textContent = 'خطأ في الاتصال بالخادم'; });
                }

                function deletePayment(id, amount) {
                    if (!confirm('حذف الدفعة بمبلغ ' + money(amount) + '؟ سيُعاد احتساب الرصيد.')) return;
                    fetch('/api/admin/billing/payments/' + encodeURIComponent(id), { method: 'DELETE' })
                        .then(function(r) { return r.json(); })
                        .then(function(d) { if (d.success) renderBilling(d.summary); });
                }

                (function() {
                    var amt = document.getElementById('payAmount');
                    if (amt) amt.addEventListener('input', function() { amt.dataset.touched = '1'; });
                })();

                // =========================================================
                // ======================= VISITORS ========================
                // =========================================================
                var visDays = 30;
                function visList(id, items, total) {
                    var ul = document.getElementById(id);
                    ul.innerHTML = '';
                    if (!items || !items.length) { ul.innerHTML = '<li style="color:#666;">لا توجد بيانات بعد</li>'; return; }
                    var max = items[0].count || 1;
                    items.forEach(function(it) {
                        var li = document.createElement('li');
                        li.innerHTML = '<span></span><b>' + it.count + '</b><em><s style="width:' + Math.max(3, Math.round(it.count / max * 100)) + '%"></s></em>';
                        li.firstChild.textContent = it.name;
                        li.firstChild.title = it.name;
                        ul.appendChild(li);
                    });
                }

                function loadVisitors(days) {
                    if (days) visDays = days;
                    document.querySelectorAll('.vis-range button').forEach(function(b) { b.classList.toggle('active', +b.dataset.days === visDays); });
                    return fetch('/api/admin/visitors?days=' + visDays).then(function(r) { return r.ok ? r.json() : null; }).then(function(d) {
                        if (!d) return;
                        document.getElementById('visToday').textContent = d.today.visits;
                        document.getElementById('visTodayU').textContent = d.today.visitors + ' زائر';
                        document.getElementById('visWeek').textContent = d.week.visits;
                        document.getElementById('visWeekU').textContent = d.week.visitors + ' زائر';
                        document.getElementById('visPeriodLabel').textContent = 'آخر ' + d.days + (d.days <= 10 ? ' أيام' : ' يوماً');
                        document.getElementById('visPeriod').textContent = d.period.visits;
                        document.getElementById('visPeriodU').textContent = d.period.visitors + ' زائر';
                        document.getElementById('visReturning').textContent = d.returning;
                        document.getElementById('visConsent').textContent = 'وافق على الكوكيز: \u2066' + d.consentRate + '%\u2069';

                        // daily bars: full bar = visits, gold part = distinct visitors
                        var chart = document.getElementById('visChart');
                        chart.innerHTML = '';
                        var max = Math.max(1, Math.max.apply(null, d.daily.map(function(x) { return x.visits; })));
                        d.daily.forEach(function(x) {
                            var bar = document.createElement('div');
                            bar.className = 'vis-bar';
                            bar.style.height = Math.max(2, x.visits / max * 100) + '%';
                            bar.title = x.date + ' — ' + x.visits + ' زيارة، ' + x.visitors + ' زائر';
                            var inner = document.createElement('i');
                            inner.style.height = (x.visits ? x.visitors / x.visits * 100 : 0) + '%';
                            bar.appendChild(inner);
                            chart.appendChild(bar);
                        });
                        var axis = chart.nextElementSibling;
                        if (!axis || !axis.classList.contains('vis-axis')) {
                            axis = document.createElement('div'); axis.className = 'vis-axis'; chart.after(axis);
                            var lg = document.createElement('div'); lg.className = 'vis-legend';
                            lg.innerHTML = '<i style="background:rgba(212,175,55,0.25)"></i>زيارات<i style="background:#D4AF37"></i>زوار';
                            axis.after(lg);
                        }
                        axis.innerHTML = '<span>' + d.daily[0].date + '</span><span>' + d.daily[d.daily.length - 1].date + '</span>';

                        visList('visPages', d.pages);
                        visList('visRefs', d.referrers);
                        visList('visDevices', d.devices);
                        visList('visBrowsers', d.browsers.concat(d.systems).sort(function(a, b) { return b.count - a.count; }).slice(0, 8));

                        var pb = document.getElementById('visPeople');
                        pb.innerHTML = '';
                        if (!d.people.length) pb.innerHTML = '<tr><td colspan="8" style="text-align:center;color:#666;padding:18px;">لا يوجد زوار معروفون بعد</td></tr>';
                        d.people.forEach(function(p) {
                            var tr = document.createElement('tr');
                            tr.innerHTML = '<td class="vis-mono">' + escapeHtml(p.id) + '</td><td class="vis-mono">' + escapeHtml(p.firstSeen) + '</td><td class="vis-mono">' + escapeHtml(p.lastSeen) + '</td>' +
                                '<td>' + p.visits + (p.days > 1 ? ' <span style="color:#D4AF37;font-size:.75rem;">(' + p.days + ' أيام)</span>' : '') + '</td>' +
                                '<td class="vis-mono">' + escapeHtml(p.pages.join('  ')) + '</td><td>' + escapeHtml(p.device) + '</td><td class="vis-mono">' + escapeHtml(p.ip) + '</td><td>' + escapeHtml(p.from) + '</td>';
                            pb.appendChild(tr);
                        });

                        var rb = document.getElementById('visRecent');
                        rb.innerHTML = '';
                        if (!d.recent.length) rb.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#666;padding:18px;">لا توجد زيارات بعد</td></tr>';
                        d.recent.forEach(function(v) {
                            var tr = document.createElement('tr');
                            tr.innerHTML = '<td class="vis-mono">' + escapeHtml(v.time) + '</td><td class="vis-mono">' + escapeHtml(v.visitor || 'غير معروف') + '</td><td class="vis-mono">' + escapeHtml(v.path) + '</td>' +
                                '<td>' + escapeHtml(v.from) + '</td><td>' + escapeHtml(v.device) + '</td><td class="vis-mono">' + escapeHtml(v.ip) + '</td>';
                            rb.appendChild(tr);
                        });
                    }).catch(function() { });
                }

                var emailInput = document.getElementById('loginEmailInput');
                var passInput = document.getElementById('loginPasswordInput');
                if (emailInput) emailInput.addEventListener('keydown', function(e) { if (e.key === 'Enter') adminLogin(); });
                if (passInput) passInput.addEventListener('keydown', function(e) { if (e.key === 'Enter') adminLogin(); });

                checkAuth();
                </script>
            </body>
            """;

            return "<!DOCTYPE html>\n<html lang=\"ar\" dir=\"rtl\">\n<head>\n<meta charset=\"utf-8\" />\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\n<title>لوحة التحكم - ME Workshop</title>\n<style>" + css + "</style>\n</head>\n" + body.Replace("%%MAPSV%%", AssetVersion) + "\n</html>";
        }
    }
}



