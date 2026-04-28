namespace Auth.Api.Dtos.Identity;

public class TelegramLinkResponseDto
{
    public string LinkCode { get; set; } = string.Empty;
    public string BotUserName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}