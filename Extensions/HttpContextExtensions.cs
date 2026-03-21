using System.Security.Claims;

namespace TaskManagementAPI.Extensions;

public static class HttpContextExtensions
{
    public static int? GetUserId(this HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return null;

        return int.Parse(userIdClaim.Value);
    }
}