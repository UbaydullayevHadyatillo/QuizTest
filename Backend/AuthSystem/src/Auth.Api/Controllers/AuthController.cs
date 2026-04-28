using Auth.Api.Dtos.Identity;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("request-link")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestLink(RequestLinkDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.CreateLinkRequestAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new
        {
            message = result.Message,
            linkCode = result.Data!.LinkCode,
            botUserName = result.Data.BotUserName,
            expiresAt = result.Data.ExpiresAt
        });
    }


    [HttpPost("request-code")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestCode(RequestCodeDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RequestLoginCodeAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("verify-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCode(VerifyCodeDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyCodeAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(dto, cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("profile")]
    public IActionResult Profile()
    {
        return Ok(new
        {
            message = "Siz tizimga kirgansiz",
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            userName = User.Identity?.Name,
            fullName = User.FindFirstValue("FullName"),
            phoneNumber = User.FindFirstValue("PhoneNumber"),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}