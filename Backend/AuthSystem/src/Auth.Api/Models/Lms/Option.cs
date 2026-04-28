namespace Auth.Api.Models.Lms;

public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; } 
    public string Text { get; set; }=string.Empty;
    public bool IsCorrect { get; set; }
}
