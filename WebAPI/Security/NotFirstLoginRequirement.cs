using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Security
{
    public sealed class NotFirstLoginRequirement : IAuthorizationRequirement { }
}