namespace Auth.Api.Models.Lms;

public class Question
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public string Text { get; set; }=string.Empty;
    public List<Option>? Options { get; internal set; }
}
