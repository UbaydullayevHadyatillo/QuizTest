using Auth.Api.Models.Identity;

namespace Auth.Api.Models;

public class VerificationCode
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string CodeHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Login";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; }
}