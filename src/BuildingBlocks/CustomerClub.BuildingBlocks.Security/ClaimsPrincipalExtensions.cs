using System.Security.Claims;

namespace CustomerClub.BuildingBlocks.Security;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClubId(this ClaimsPrincipal principal)
        => principal.FindFirstValue("club_id") ?? principal.FindFirstValue("tenant_id");

    public static string? GetActorId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
}
