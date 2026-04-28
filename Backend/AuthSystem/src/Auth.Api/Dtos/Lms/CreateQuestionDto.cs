using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Lms;

public class CreateQuestionDto
{
    [Required]
    public int SubjectId { get; set; }

    [Required]
    [MinLength(10, ErrorMessage = "Savol matni juda qisqa")]
    public string Text { get; set; } = string.Empty;

    public List<CreateOptionDto> Options { get; set; } = new List<CreateOptionDto>();
}
