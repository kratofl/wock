namespace Wock.Common.Domain;

public interface IUserOwnedEntity
{
    string? OwnerUserId { get; set; }
}
