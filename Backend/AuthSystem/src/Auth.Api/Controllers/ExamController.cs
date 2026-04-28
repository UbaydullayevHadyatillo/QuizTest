using Auth.Api.Data;
using Auth.Api.Dtos.Lms; 
using Auth.Api.Models.Lms; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExamController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ExamController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("start/{subjectId}")]
    public async Task<IActionResult> GetQuestions(int subjectId)
    {
        var allQuestions = await _dbContext.Questions
            .Where(q => q.SubjectId == subjectId)
            .Include(q => q.Options)
            .ToListAsync();

        if (allQuestions == null || !allQuestions.Any())
            return BadRequest("Bu fanda hali savollar yo'q.");

        var randomQuestions = allQuestions
            .OrderBy(q => Guid.NewGuid())
            .Take(10)
            .Select(q => new
            {
                q.Id,
                q.Text,
                Options = q.Options != null
                    ? q.Options.OrderBy(o => Guid.NewGuid()).Select(o => new { o.Id, o.Text })
                    : null
            })
            .ToList();

        return Ok(randomQuestions);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit(SumbitExamDto dto) 
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var userId = int.Parse(userIdClaim);

        int correctCount = 0;

        if (dto.Answer == null || !dto.Answer.Any())
            return BadRequest("Javoblar yuborilmadi.");

        int totalQuestions = dto.Answer.Count;

        foreach (var answer in dto.Answer)
        {
            var correctOption = await _dbContext.Options
                .FirstOrDefaultAsync(o => o.QuestionId == answer.QuestionId && o.IsCorrect);

            if (correctOption != null && correctOption.Id == answer.SelectedOptionId)
            {
                correctCount++;
            }
        }

        double percentage = totalQuestions > 0 ? (double)correctCount / totalQuestions * 100 : 0;

        var result = new ExamResult
        {
            UserId = userId,
            SubjectId = dto.SubjectId,
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctCount,
            Percentage = Math.Round(percentage, 2),
            FinishedAt = DateTime.UtcNow
        };

        _dbContext.ExamResults.Add(result);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Imtihon yakunlandi",
            correctAnswers = correctCount,
            total = totalQuestions,
            score = Math.Round(percentage, 2)
        });
    }
}