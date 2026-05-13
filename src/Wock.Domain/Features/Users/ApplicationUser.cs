namespace Wock.Features.Users.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public required string UserName { get; set; }

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<ApplicationUserRole> Roles { get; set; } = [];
}
