// =============================================================================
// ME Workshop — monthly website upkeep, "الإدامة الشهرية" (billing)
// A fixed monthly charge that renews on the same day each month, starting on
// StartDate. Unpaid amounts carry forward. Payments are recorded by an Admin and
// are applied to the oldest open month first. Stored in <data>/billing.json.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ConsanWebApp
{
    public class BillingPayment
    {
        public string Id { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaidOn { get; set; } = "";      // yyyy-MM-dd
        public string Note { get; set; } = "";
        public string RecordedBy { get; set; } = "";
        public string RecordedAt { get; set; } = "";
    }

    public class BillingData
    {
        public decimal MonthlyAmount { get; set; } = 20m;
        public string Currency { get; set; } = "USD";
        public string StartDate { get; set; } = "2026-11-13"; // first invoice; renews on this day every month
        public List<BillingPayment> Payments { get; set; } = new List<BillingPayment>();
    }

    public static partial class Store
    {
        private static readonly object BillingLock = new object();
        private static BillingData? _billing;
        private static string BillingPath { get { return System.IO.Path.Combine(_dataDir, "billing.json"); } }

        private static BillingData Billing()
        {
            if (_billing == null) _billing = LoadOrDefault(BillingPath, () => new BillingData());
            return _billing;
        }

        public static BillingData GetBilling()
        {
            lock (BillingLock)
            {
                var b = Billing();
                return new BillingData
                {
                    MonthlyAmount = b.MonthlyAmount,
                    Currency = b.Currency,
                    StartDate = b.StartDate,
                    Payments = b.Payments.ToList()
                };
            }
        }

        public static BillingPayment AddBillingPayment(decimal amount, string paidOn, string note, string by)
        {
            lock (BillingLock)
            {
                var p = new BillingPayment
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 12),
                    Amount = Math.Round(amount, 2),
                    PaidOn = paidOn,
                    Note = note,
                    RecordedBy = by,
                    RecordedAt = DateTime.UtcNow.ToString("o")
                };
                Billing().Payments.Add(p);
                Persist(BillingPath, _billing);
                return p;
            }
        }

        public static bool DeleteBillingPayment(string id)
        {
            lock (BillingLock)
            {
                int n = Billing().Payments.RemoveAll(p => p.Id == id);
                if (n > 0) Persist(BillingPath, _billing);
                return n > 0;
            }
        }
    }

    public static class BillingFeature
    {
        // The workshop is in Iraq (UTC+3, no daylight saving): a new month starts at local midnight.
        public static DateTime Today() { return DateTime.UtcNow.AddHours(3).Date; }

        private static DateTime ParseDate(string s)
        {
            return DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string D(DateTime d) { return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }

        public static object Summary(BillingData b, DateTime today)
        {
            var start = ParseDate(b.StartDate);
            // A month's invoice opens on start + k months (AddMonths keeps the day, or the last day of a short month).
            int cycles = 0;
            while (start.AddMonths(cycles) <= today) cycles++;

            // Each month: website maintenance, plus Orion's reserve and any extra usage that month.
            decimal amount = b.MonthlyAmount;
            var months = Enumerable.Range(0, cycles).Select(k => { var ai = AiBilling.Charges(start.AddMonths(k)); return (maint: amount, reserve: ai.reserve, extra: ai.extra, plan: ai.plan); }).ToList();
            decimal MonthTotal(int k) => months[k].maint + months[k].reserve + months[k].extra;
            decimal paid = b.Payments.Sum(p => p.Amount);
            decimal billed = Enumerable.Range(0, cycles).Sum(MonthTotal);
            decimal balance = Math.Max(0, billed - paid);
            decimal credit = Math.Max(0, paid - billed);

            // Apply payments to the oldest month first.
            var invoices = new List<object>();
            decimal pool = paid;
            for (int k = 0; k < cycles; k++)
            {
                var from = start.AddMonths(k);
                var to = start.AddMonths(k + 1).AddDays(-1);
                decimal total = MonthTotal(k);
                decimal applied = Math.Min(total, Math.Max(0, pool));
                pool -= applied;
                string status = applied >= total ? "paid" : (applied > 0 ? "partial" : (k == cycles - 1 ? "due" : "overdue"));
                invoices.Add(new
                {
                    number = "INV-" + from.ToString("yyyyMM", CultureInfo.InvariantCulture),
                    periodStart = D(from),
                    periodEnd = D(to),
                    dueDate = D(from),
                    amount = total,
                    maintenance = months[k].maint,
                    orionReserve = months[k].reserve,
                    orionExtra = months[k].extra,
                    paid = applied,
                    remaining = total - applied,
                    status
                });
            }
            invoices.Reverse(); // newest first

            DateTime currentStart = cycles > 0 ? start.AddMonths(cycles - 1) : start;
            DateTime nextRenewal = cycles > 0 ? start.AddMonths(cycles) : start;
            decimal currentTotal = cycles > 0 ? MonthTotal(cycles - 1) : 0;
            decimal previousBalance = Math.Max(0, balance - currentTotal);
            int monthsOpen = 0;
            { decimal rest = balance; for (int k = cycles - 1; k >= 0 && rest > 0; k--) { monthsOpen++; rest -= MonthTotal(k); } }
            var nextAi = AiBilling.Charges(nextRenewal);

            string state = cycles == 0 ? "upcoming"
                : balance <= 0 ? "paid"
                : monthsOpen > 1 ? "overdue"
                : "due";

            return new
            {
                plan = "الإدامة الشهرية للموقع",
                currency = b.Currency,
                maintenanceAmount = amount,
                // the fixed part of next month's invoice (maintenance + Orion reserve), extra usage not included
                monthlyAmount = amount + nextAi.reserve,
                currentMaintenance = cycles > 0 ? months[cycles - 1].maint : amount,
                currentOrionReserve = cycles > 0 ? months[cycles - 1].reserve : 0,
                currentOrionExtra = cycles > 0 ? months[cycles - 1].extra : 0,
                currentTotal,
                startDate = b.StartDate,
                today = D(today),
                currentInvoice = cycles > 0 ? "INV-" + currentStart.ToString("yyyyMM", CultureInfo.InvariantCulture) : "",
                currentPeriodStart = D(currentStart),
                currentPeriodEnd = D(nextRenewal.AddDays(-1)),
                dueDate = D(currentStart),
                nextRenewal = D(nextRenewal),
                totalBilled = billed,
                totalPaid = paid,
                previousBalance,
                balance,
                credit,
                monthsOpen,
                status = state,
                invoices,
                payments = b.Payments.OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.RecordedAt).ToList()
            };
        }

        public static void MapBilling(WebApplication app)
        {
            // Admin and Manager can see the account; only an Admin records or removes payments.
            app.MapGet("/api/admin/billing", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var summary = Summary(Store.GetBilling(), Today());
                return Results.Json(new { canEdit = AuthExtensions.IsAdminOrLegacy(ctx), summary });
            });

            app.MapPost("/api/admin/billing/payments", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "غير مصرح — Admin فقط" }, statusCode: 403);
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("amount", out var amountText);
                body.TryGetValue("paidOn", out var paidOn);
                body.TryGetValue("note", out var note);

                if (!decimal.TryParse((amountText ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0 || amount > 100000)
                    return Results.Json(new { message = "أدخل مبلغاً صحيحاً أكبر من صفر" }, statusCode: 400);
                if (decimal.Round(amount, 2) != amount)
                    return Results.Json(new { message = "المبلغ يقبل منزلتين عشريتين فقط" }, statusCode: 400);

                paidOn = string.IsNullOrWhiteSpace(paidOn) ? D(Today()) : paidOn.Trim();
                if (!DateTime.TryParseExact(paidOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var paidDate) || paidDate > Today().AddDays(1))
                    return Results.Json(new { message = "تاريخ الدفع غير صحيح" }, statusCode: 400);

                note = (note ?? "").Trim();
                if (note.Length > 200) note = note.Substring(0, 200);
                string by = ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin";

                var p = Store.AddBillingPayment(amount, paidOn, note, by);
                return Results.Json(new { success = true, payment = p, summary = Summary(Store.GetBilling(), Today()) });
            });

            app.MapDelete("/api/admin/billing/payments/{id}", (HttpContext ctx, string id) =>
            {
                if (!AuthExtensions.IsAdminOrLegacy(ctx)) return Results.Json(new { message = "غير مصرح — Admin فقط" }, statusCode: 403);
                return Store.DeleteBillingPayment(id)
                    ? Results.Json(new { success = true, summary = Summary(Store.GetBilling(), Today()) })
                    : Results.Json(new { message = "not found" }, statusCode: 404);
            });
        }
    }
}
