namespace Auth.Api.Options;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public int AccessTokenMinures { get; set; } = 5;
    public int RefreshTokenDays { get; set; } = 7;
    public double AccessTokenMinutes { get; internal set; }
}
