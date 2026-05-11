namespace Wock.Common.Security;

public sealed class AnonymousCurrentUserContext : ICurrentUserContext
{
    public static AnonymousCurrentUserContext Instance { get; } = new();

    private AnonymousCurrentUserContext()
    {
    }

    public string? UserId => null;

    public string? DisplayName => null;

    public bool IsAuthenticated => false;
}
