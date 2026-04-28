using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Identity;

public class RequestLinkDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty; // username yoki phone
}