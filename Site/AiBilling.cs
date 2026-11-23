// =============================================================================
// ME Workshop — Orion usage plans (part of "الإدامة الشهرية")
//
// The owner sees: a fixed monthly Orion reserve ($10 standard / $20 upgraded) that
// covers a number of reserved conversations, a switch for extra usage, and any extra
// conversations billed at the same per-conversation price.
//
// Internally every reply is metered at its real Gemini token cost. A plan's reserve
// covers model cost up to ReserveCostShare of its price; that cost budget divided by
// CostPerConversation gives the reserved-conversation count shown to the owner. Extra
// conversations are billed at the plan's per-conversation price, i.e. ExtraMultiplier
// times their model cost. None of the cost figures leave the server.
// Stored in <data>/ai-usage.json, one record per billing month.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ConsanWebApp
{
    public class AiCycle
    {
        public string Start { get; set; } = "";          // billing month start, yyyy-MM-dd
        public string Plan { get; set; } = "standard";
        public bool AllowExtra { get; set; }
        public decimal Cost { get; set; }                // model cost this month, USD (internal)
        public decimal ExtraCost { get; set; }           // the part of Cost incurred past the reserve with extra usage on (internal)
        public int Replies { get; set; }
        public List<string> Sids { get; set; } = new List<string>();
        public string PausedAt { get; set; } = "";       // when the reserve ran out with extra usage off
    }

    public class AiUsageData
    {
        public string Plan { get; set; } = "standard";
        public bool AllowExtra { get; set; } = false;
        // Orion charges start with the billing month that begins on or after this date;
        // earlier months run as a trial: the standard reserve, nothing billed, no extra usage.
        public string BillingFrom { get; set; } = "2026-12-13";
        public Dictionary<string, AiCycle> Cycles { get; set; } = new Dictionary<string, AiCycle>();
    }

    public static partial class Store
    {
        private static readonly object AiUsageLock = new object();
        private static AiUsageData? _aiUsage;
        private static string AiUsagePath { get { return System.IO.Path.Combine(_dataDir, "ai-usage.json"); } }

        internal static T WithAiUsage<T>(Func<AiUsageData, T> f, bool save = false)
        {
            lock (AiUsageLock)
            {
                if (_aiUsage == null) _aiUsage = LoadOrDefault(AiUsagePath, () => new AiUsageData());
                var r = f(_aiUsage);
                if (save) Persist(AiUsagePath, _aiUsage);
                return r;
            }
        }
    }

    public static class AiBilling
    {
        // ---- plans (what the owner sees) -----------------------------------------
        public sealed class PlanInfo { public string Id = ""; public string Name = ""; public decimal Reserve; }
        public static readonly PlanInfo[] Plans =
        {
            new PlanInfo { Id = "standard", Name = "الأساسية", Reserve = 10m },
            new PlanInfo { Id = "upgraded", Name = "الموسّعة", Reserve = 20m }
        };
        public static PlanInfo PlanOf(string id) => Plans.FirstOrDefault(p => p.Id == id) ?? Plans[0];

        // ---- internal economics (never sent to the browser) ----------------------
        private const decimal ReserveCostShare = 0.5m;        // model cost covered by a reserve: half its price
        private const decimal ExtraMultiplier = 2m;           // extra usage is billed at twice its model cost
        private const decimal CostPerConversation = 0.0025m;  // model cost of one reserved conversation (a few questions and replies)

        // USD per 1M tokens (input, output). Unknown models are priced at the dearest known rate.
        private static readonly Dictionary<string, (decimal inp, decimal outp)> Prices = new Dictionary<string, (decimal, decimal)>
        {
            ["gemini-3.1-flash-lite"] = (0.25m, 1.50m),
            ["gemini-3.5-flash-lite"] = (0.30m, 2.50m),
            ["gemini-flash-lite-latest"] = (0.30m, 2.50m),
            ["gemini-3.5-flash"] = (1.50m, 9.00m),
            ["gemini-3-flash-preview"] = (0.50m, 3.00m)
        };

        public static decimal CostOf(string model, int tokIn, int tokOut)
        {
            var p = Prices.TryGetValue(model ?? "", out var v) ? v : Prices.Values.OrderByDescending(x => x.outp).First();
            return (tokIn * p.inp + tokOut * p.outp) / 1_000_000m;
        }

        private static decimal CostCap(PlanInfo plan) => plan.Reserve * ReserveCostShare;
        public static int ReservedConversations(PlanInfo plan) => (int)Math.Floor(CostCap(plan) / CostPerConversation);
        public static decimal PricePerConversation(PlanInfo plan) => plan.Reserve / ReservedConversations(plan);

        // ---- billing months follow the website invoice (same start day) -----------
        public static DateTime CycleStart(DateTime today)
        {
            var start = DateTime.ParseExact(Store.GetBilling().StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (today < start) return start;
            int k = 0;
            while (start.AddMonths(k + 1) <= today) k++;
            return start.AddMonths(k);
        }
        private static string D(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static bool Billable(AiUsageData u, string cycleStart) => string.CompareOrdinal(cycleStart, u.BillingFrom) >= 0;

        private static AiCycle Current(AiUsageData u, DateTime today)
        {
            string key = D(CycleStart(today));
            if (!u.Cycles.TryGetValue(key, out var c))
            {
                c = new AiCycle { Start = key, Plan = u.Plan, AllowExtra = u.AllowExtra };
                u.Cycles[key] = c;
            }
            return c;
        }

        // Extra usage only counts in a billed month with the switch on.
        private static bool ExtraOn(AiUsageData u, AiCycle c) => c.AllowExtra && Billable(u, c.Start);

        // ---- metering ------------------------------------------------------------
        public static bool Available()
        {
            return Store.WithAiUsage(u =>
            {
                var c = Current(u, BillingFeature.Today());
                return c.Cost < CostCap(PlanOf(c.Plan)) || ExtraOn(u, c);
            });
        }

        public static void Record(decimal cost, string sid)
        {
            if (cost <= 0 && string.IsNullOrEmpty(sid)) return;
            Store.WithAiUsage(u =>
            {
                var c = Current(u, BillingFeature.Today());
                decimal cap = CostCap(PlanOf(c.Plan));
                decimal before = c.Cost;
                c.Cost += cost;
                c.Replies++;
                if (!string.IsNullOrEmpty(sid) && sid != "anon" && !c.Sids.Contains(sid) && c.Sids.Count < 50000) c.Sids.Add(sid);
                if (ExtraOn(u, c))
                    c.ExtraCost += Math.Max(0, c.Cost - Math.Max(cap, before));
                else if (c.Cost >= cap && c.PausedAt.Length == 0)
                    c.PausedAt = DateTime.UtcNow.ToString("o");
                return true;
            }, save: true);
        }

        // What a billing month adds to the invoice: the reserve and any extra usage.
        public static (decimal reserve, decimal extra, string plan) Charges(DateTime cycleStart)
        {
            return Store.WithAiUsage(u =>
            {
                string key = D(cycleStart);
                if (!Billable(u, key)) return (0m, 0m, "");
                bool isCurrent = key == D(CycleStart(BillingFeature.Today()));
                AiCycle? c = u.Cycles.TryGetValue(key, out var found) ? found : null;
                var plan = PlanOf(c?.Plan ?? (isCurrent || string.CompareOrdinal(key, D(BillingFeature.Today())) > 0 ? u.Plan : "standard"));
                decimal extra = c == null ? 0m : Math.Round(c.ExtraCost * ExtraMultiplier, 2);
                return (plan.Reserve, extra, plan.Id);
            });
        }

        // The owner's view: conversations and prices, no costs.
        public static object OwnerView()
        {
            var today = BillingFeature.Today();
            return Store.WithAiUsage(u =>
            {
                var c = Current(u, today);
                var plan = PlanOf(c.Plan);
                int reserved = ReservedConversations(plan);
                decimal unit = PricePerConversation(plan);
                int used = (int)Math.Round(c.Cost / CostPerConversation);
                int extraConv = (int)Math.Round(c.ExtraCost / CostPerConversation);
                bool billable = Billable(u, c.Start);
                bool active = c.Cost < CostCap(plan) || ExtraOn(u, c);
                var start = DateTime.ParseExact(c.Start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                return new
                {
                    plan = plan.Id,
                    nextPlan = u.Plan,
                    allowExtra = c.AllowExtra,
                    trial = !billable,
                    billingFrom = u.BillingFrom,
                    periodStart = c.Start,
                    periodEnd = D(start.AddMonths(1).AddDays(-1)),
                    reserveAmount = plan.Reserve,
                    reservedConversations = reserved,
                    usedConversations = Math.Min(used, reserved),
                    remainingConversations = Math.Max(0, reserved - used),
                    extraConversations = billable ? extraConv : 0,
                    extraAmount = billable ? Math.Round(c.ExtraCost * ExtraMultiplier, 2) : 0m,
                    pricePerConversation = Math.Round(unit, 4),
                    pricePer100 = Math.Round(unit * 100, 2),
                    percentUsed = reserved > 0 ? Math.Min(100, (int)Math.Round(100.0 * used / reserved)) : 0,
                    active,
                    replies = c.Replies,
                    visitors = c.Sids.Count,
                    plans = Plans.Select(p => new { id = p.Id, name = p.Name, reserve = p.Reserve, conversations = ReservedConversations(p), pricePer100 = Math.Round(PricePerConversation(p) * 100, 2) })
                };
            });
        }

        public static void MapAiBilling(WebApplication app)
        {
            app.MapGet("/api/admin/orion-usage", (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                return Results.Json(OwnerView());
            });

            // Plan and extra-usage switch. The switch and upgrades apply right away; a downgrade from next month.
            app.MapPut("/api/admin/orion-usage", async (HttpContext ctx) =>
            {
                if (!AuthExtensions.IsDashboardUser(ctx)) return Results.Json(new { message = "unauthorized" }, statusCode: 401);
                var body = await Store.ReadJsonDict(ctx);
                body.TryGetValue("plan", out var plan);
                body.TryGetValue("allowExtra", out var extra);
                if (plan != null && !Plans.Any(p => p.Id == plan)) return Results.Json(new { message = "خطة غير معروفة" }, statusCode: 400);
                Store.WithAiUsage(u =>
                {
                    var c = Current(u, BillingFeature.Today());
                    // An upgrade applies to this month at once; a downgrade starts next month.
                    if (plan != null)
                    {
                        u.Plan = plan;
                        if (PlanOf(plan).Reserve >= PlanOf(c.Plan).Reserve)
                        {
                            c.Plan = plan;
                            // Extra usage that now fits inside the bigger reserve is no longer billed as extra.
                            c.ExtraCost = Math.Min(c.ExtraCost, Math.Max(0, c.Cost - CostCap(PlanOf(plan))));
                        }
                    }
                    if (extra != null) { bool on = extra == "true" || extra == "1"; u.AllowExtra = on; c.AllowExtra = on; }
                    if (c.Cost < CostCap(PlanOf(c.Plan)) || ExtraOn(u, c)) c.PausedAt = "";
                    return true;
                }, save: true);
                return Results.Json(new { success = true, usage = OwnerView() });
            });
        }
    }
}
