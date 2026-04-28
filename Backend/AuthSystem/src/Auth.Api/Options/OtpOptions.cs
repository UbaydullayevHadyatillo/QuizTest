namespace Auth.Api.Options;

public class OtpOptions
{
    public int Length { get; set; } = 6;
    public int CodeExpiryMinutes { get; set; } = 2;
    public int MaxAttempts { get; set; } = 5;
    public int LinkCodeExpiryMinutes { get; set; } = 10;
}
