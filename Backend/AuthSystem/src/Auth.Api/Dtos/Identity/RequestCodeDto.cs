using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Identity;

public class RequestCodeDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty; // username yoki phone
}