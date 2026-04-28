using Auth.Api.Models.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Api.Models.Lms;

public class ExamResult
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public AppUser User { get; set; } = null!;

    public int SubjectId { get; set; }
    [ForeignKey("SubjectId")]
    public Subject Subject { get; set; } = null!;

    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double Percentage { get; set; }
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;

    // Har bir savolga berilgan javoblar tarixi 
    public List<TestResult> Details { get; set; } = new();
}
