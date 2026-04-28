using Auth.Api.Data;
using Auth.Api.Dtos.Lms;
using Auth.Api.Models.Lms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]

public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(CreateSubjectDto dto)
    {
        var subject = new Subject
        {
            Name = dto.Name
        };
        _dbContext.Subjects.Add(subject);
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Fan yaratildi ", id = subject.Id });
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSUbject()
    {
        var subjects = await _dbContext.Subjects.ToListAsync();
        return Ok(subjects);
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
    {
        var findSubject = await _dbContext.Subjects.FirstOrDefaultAsync(s => s.Id == dto.SubjectId);

        if (findSubject == null)
        {
            return BadRequest($"Bunday fan topilmadi. Siz yuborgan ID: {dto.SubjectId}");
        }

        var question = new Question
        {
            SubjectId = dto.SubjectId,
            Text = dto.Text,
            Options = dto.Options.Select(o => new Auth.Api.Models.Lms.Option
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList()
        };

        _dbContext.Questions.Add(question);
        await _dbContext.SaveChangesAsync();

        return Ok("Savol muvaffaqiyatli qo'shildi.");
    }

    [HttpGet("questions/{subjectId}")]
    public async Task<IActionResult> GetQuestionsBySubject(int subjectId)
    {
        var questions = await _dbContext.Questions
            .Where(q=>q.SubjectId == subjectId)
            .Include(q=>q.Options)
            .ToListAsync();
        return Ok(questions);
    }
    
}
