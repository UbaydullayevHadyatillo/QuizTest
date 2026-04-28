namespace Auth.Api.Models.Identity;

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "User";

    public long? TelegramChatId { get; set; }
    public bool IsTelegramVerified { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
    public ICollection<TelegramLinkRequest> TelegramLinkRequests { get; set; } = new List<TelegramLinkRequest>();
}