using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Identity;

public class LogoutDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}