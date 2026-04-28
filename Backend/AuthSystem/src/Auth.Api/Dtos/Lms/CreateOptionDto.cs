using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Lms;

public class CreateOptionDto
{
    [Required]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}