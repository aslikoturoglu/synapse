using System.Security.Claims;

namespace Server.Controllers;

public static class CurrentUserExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
