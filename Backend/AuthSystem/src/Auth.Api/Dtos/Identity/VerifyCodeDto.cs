using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Identity;

public class VerifyCodeDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}