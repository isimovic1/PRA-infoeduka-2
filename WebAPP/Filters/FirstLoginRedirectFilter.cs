using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WebApp.Filters
{
    public class FirstLoginRedirectFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (!(user?.Identity?.IsAuthenticated ?? false)) return;

            var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
            // allow auth pages even during first login
            var allowed = path.StartsWith("/auth/login") ||
                          path.StartsWith("/auth/logout") ||
                          path.StartsWith("/auth/changepassword") ||
                          path.StartsWith("/auth/accessdenied");

            if (allowed) return;

            var isFirst = user.FindFirst("IsFirstLogin")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (isFirst)
            {
                context.Result = new RedirectToActionResult("ChangePassword", "Auth", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
