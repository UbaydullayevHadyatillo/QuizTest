using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Dtos.Lms;

public class CreateSubjectDto
{
    [Required(ErrorMessage = "Fan nomi kiritilishi shart")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
