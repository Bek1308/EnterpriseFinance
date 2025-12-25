using Microsoft.EntityFrameworkCore;
using EnterpriseFinance.Data;

namespace EnterpriseFinance.Middleware
{
    public class TenantResolverMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolverMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            int tenantId = 1; // Default – birinchi korxona

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null && user.TenantId > 0)
                {
                    tenantId = user.TenantId;
                }
            }

            // HttpContext ga saqlaymiz – keyin DbContext ishlatadi
            context.Items["TenantId"] = tenantId;

            await _next(context);
        }
    }

    // Extension method – Program.cs da qulay ishlatish uchun
    public static class TenantResolverMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantResolver(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantResolverMiddleware>();
        }
    }
}