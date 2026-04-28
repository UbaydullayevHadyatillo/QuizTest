using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Api.Models.Lms;

public class TestResult
{
    public int Id { get; set; }

    public int ExamResultId { get; set; }
    [ForeignKey("ExamResultId")]
    public ExamResult ExamResult { get; set; } = null!;

    public int QuestionId { get; set; }
    [ForeignKey("QuestionId")]
    public Question Question { get; set; } = null!;

    public int SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}
