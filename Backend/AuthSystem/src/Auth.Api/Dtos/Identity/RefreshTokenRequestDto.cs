using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Identity;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}