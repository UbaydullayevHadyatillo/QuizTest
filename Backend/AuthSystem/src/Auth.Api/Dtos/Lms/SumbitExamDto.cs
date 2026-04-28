namespace Auth.Api.Dtos.Lms;

public class SumbitExamDto
{
    public int SubjectId { get; set; }
    public List<UserAnswerDto> Answer { get; set; } = new();
}
