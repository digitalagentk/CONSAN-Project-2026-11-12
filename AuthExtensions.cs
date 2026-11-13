using Microsoft.AspNetCore.Http;

namespace ConsanWebApp
{
    public static class AuthExtensions
    {
        /// <summary>
        /// يتحقق من أن المستخدم أدمن (سواء بالنظام الجديد أو القديم)
        /// </summary>
        public static bool IsAdminOrLegacy(HttpContext ctx)
        {
            if (ctx.User?.IsInRole("Admin") == true) return true;
            return Store.IsAdminAuthenticated(ctx);
        }

        /// <summary>
        /// يتحقق من أن المستخدم أدمن أو مدير
        /// </summary>
        public static bool IsDashboardUser(HttpContext ctx)
        {
            if (ctx.User?.IsInRole("Admin") == true) return true;
            if (ctx.User?.IsInRole("Manager") == true) return true;
            return Store.IsAdminAuthenticated(ctx);
        }
    }
}
