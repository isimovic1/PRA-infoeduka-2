using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Models; // Infoeduka2Context

namespace WebAPI.Security
{
    public sealed class NotFirstLoginHandler : AuthorizationHandler<NotFirstLoginRequirement>
    {
        private readonly Infoeduka2Context _db;

        public NotFirstLoginHandler(Infoeduka2Context db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotFirstLoginRequirement requirement)
        {
            // Try common claim types for email
            var email =
                (context.User.FindFirst(ClaimTypes.Name)?.Value) ??
                (context.User.FindFirst(ClaimTypes.Email)?.Value) ??
                (context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value) ??
                (context.User.FindFirst("sub")?.Value);

            if (string.IsNullOrWhiteSpace(email))
                return; // no success -> stays unauthorized

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
                return;

            if (!user.IsFirstLogin)
            {
                context.Succeed(requirement);
            }
            // else: do nothing -> requirement not met
        }
    }
}
