using System.Security.Claims;

namespace Wock.Common.Security;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => IsAuthenticated
        ? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;

    public string? DisplayName => IsAuthenticated
        ? User?.Identity?.Name ?? User?.FindFirstValue(ClaimTypes.Email)
        : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
