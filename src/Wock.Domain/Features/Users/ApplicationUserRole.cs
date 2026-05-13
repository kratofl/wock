using Wock.Common.Domain;

namespace Wock.Features.Users.Models;

public class ApplicationUserRole : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public ApplicationRole Role { get; set; }
}
