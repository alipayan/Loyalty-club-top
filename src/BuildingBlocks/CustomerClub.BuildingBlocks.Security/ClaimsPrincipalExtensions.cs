using System.Security.Claims;

namespace CustomerClub.BuildingBlocks.Security;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClubId(this ClaimsPrincipal principal)
        => principal.FindFirst("club_id")?.Value
           ?? principal.FindFirst("tenant_id")?.Value;

    public static string? GetActorId(this ClaimsPrincipal principal)
        => principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? principal.FindFirst("sub")?.Value;
}