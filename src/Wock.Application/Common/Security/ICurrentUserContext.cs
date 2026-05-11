namespace Wock.Common.Security;

public interface ICurrentUserContext
{
    string? UserId { get; }

    string? DisplayName { get; }

    bool IsAuthenticated { get; }
}
