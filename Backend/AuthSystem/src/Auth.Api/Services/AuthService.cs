using Auth.Api.Common;
using Auth.Api.Data;
using Auth.Api.Dtos.Identity;
using Auth.Api.Models;
using Auth.Api.Models.Identity;
using Auth.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public class AuthService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtService _jwtService;
    private readonly OtpService _otpService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly TelegramBotService _telegramBotService;
    private readonly TelegramOptions _telegramOptions;
    private readonly OtpOptions _otpOptions;

    public AuthService(
        AppDbContext dbContext,
        JwtService jwtService,
        OtpService otpService,
        RefreshTokenService refreshTokenService,
        TelegramBotService telegramBotService,
        IOptions<TelegramOptions> telegramOptions,
        IOptions<OtpOptions> otpOptions)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _otpService = otpService;
        _refreshTokenService = refreshTokenService;
        _telegramBotService = telegramBotService;
        _telegramOptions = telegramOptions.Value;
        _otpOptions = otpOptions.Value;
    }

    public async Task<ServiceResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var userName = dto.UserName.Trim();
        var phoneNumber = dto.PhoneNumber.Trim();

        var userNameExists = await _dbContext.Users.AnyAsync(x => x.UserName == userName, cancellationToken);
        if (userNameExists)
            return new ServiceResult(false, "Bu username band.");

        var phoneExists = await _dbContext.Users.AnyAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
        if (phoneExists)
            return new ServiceResult(false, "Bu telefon raqam allaqachon ishlatilgan.");

        var user = new AppUser
        {
            FullName = dto.FullName.Trim(),
            UserName = userName,
            PhoneNumber = phoneNumber,
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceResult(true, "Foydalanuvchi muvaffaqiyatli ro'yxatdan o'tdi.");
    }

    public async Task<ServiceResult<TelegramLinkResponseDto>> CreateLinkRequestAsync(RequestLinkDto dto, CancellationToken cancellationToken = default)
    {
        var user = await FindUserByIdentifierAsync(dto.Identifier, cancellationToken);
        if (user is null)
            return new ServiceResult<TelegramLinkResponseDto>(false, "Foydalanuvchi topilmadi.");

        if (!user.IsActive)
            return new ServiceResult<TelegramLinkResponseDto>(false, "Foydalanuvchi faol emas.");

        var activeLinks = await _dbContext.TelegramLinkRequests
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var item in activeLinks)
        {
            item.IsUsed = true;
            item.UsedAt = DateTime.UtcNow;
        }

        var linkCode = _otpService.GenerateLinkCode();

        var linkRequest = new TelegramLinkRequest
        {
            UserId = user.Id,
            LinkCode = linkCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otpOptions.LinkCodeExpiryMinutes)
        };

        _dbContext.TelegramLinkRequests.Add(linkRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new TelegramLinkResponseDto
        {
            LinkCode = linkCode,
            BotUserName = _telegramOptions.BotUserName,
            ExpiresAt = linkRequest.ExpiresAt
        };

        return new ServiceResult<TelegramLinkResponseDto>(
            true,
            "Telegram bog'lash kodi yaratildi.",
            response);
    }

    public async Task<ServiceResult> RequestLoginCodeAsync(RequestCodeDto dto, CancellationToken cancellationToken = default)
    {
        var user = await FindUserByIdentifierAsync(dto.Identifier, cancellationToken);
        if (user is null)
            return new ServiceResult(false, "Foydalanuvchi topilmadi.");

        if (!user.IsActive)
            return new ServiceResult(false, "Foydalanuvchi bloklangan yoki faol emas.");

        if (!user.IsTelegramVerified || user.TelegramChatId is null)
            return new ServiceResult(false, "Telegram hali bog'lanmagan.");

        var (plainCode, entity) = await _otpService.CreateLoginCodeAsync(user, cancellationToken);

        var text =
            $"Tasdiqlash kodi: {plainCode}\n" +
            $"Amal qilish muddati: {_otpOptions.CodeExpiryMinutes} minut.\n" +
            "Agar bu so'rovni siz yubormagan bo'lsangiz, kodni hech kimga bermang.";

        var sent = await _telegramBotService.SendMessageAsync(user.TelegramChatId.Value, text, cancellationToken);
        if (!sent)
            return new ServiceResult(false, "Telegramga kod yuborilmadi. Bot token yoki chat bog'lanishini tekshiring.");

        return new ServiceResult(true, $"Kod Telegramga yuborildi. Amal qilish vaqti: {entity.ExpiresAt:u}");
    }

    public async Task<ServiceResult<AuthResponseDto>> VerifyCodeAsync(VerifyCodeDto dto, CancellationToken cancellationToken = default)
    {
        var user = await FindUserByIdentifierAsync(dto.Identifier, cancellationToken);
        if (user is null)
            return new ServiceResult<AuthResponseDto>(false, "Foydalanuvchi topilmadi.");

        var verified = await _otpService.VerifyLoginCodeAsync(user, dto.Code.Trim(), cancellationToken);
        if (!verified)
            return new ServiceResult<AuthResponseDto>(false, "Kod xato, eskirgan yoki ishlatilgan.");

        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user, cancellationToken);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role
        };

        return new ServiceResult<AuthResponseDto>(true, "Tizimga muvaffaqiyatli kirildi.", response);
    }

    public async Task<ServiceResult<AuthResponseDto>> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenService.GetByTokenAsync(dto.RefreshToken.Trim(), cancellationToken);
        if (refreshToken is null)
            return new ServiceResult<AuthResponseDto>(false, "Refresh token topilmadi.");

        if (!refreshToken.IsActive)
            return new ServiceResult<AuthResponseDto>(false, "Refresh token yaroqsiz yoki revoke qilingan.");

        if (!refreshToken.User.IsActive)
            return new ServiceResult<AuthResponseDto>(false, "Foydalanuvchi faol emas.");

        var newRefreshToken = await _refreshTokenService.RotateAsync(refreshToken, cancellationToken);
        var newAccessToken = _jwtService.GenerateAccessToken(refreshToken.User);

        var response = new AuthResponseDto
        {
            AccessToken = newAccessToken.Token,
            AccessTokenExpiresAt = newAccessToken.ExpiresAt,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
            UserName = refreshToken.User.UserName,
            FullName = refreshToken.User.FullName,
            Role = refreshToken.User.Role
        };

        return new ServiceResult<AuthResponseDto>(true, "Tokenlar yangilandi.", response);
    }

    public async Task<ServiceResult> LogoutAsync(LogoutDto dto, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenService.GetByTokenAsync(dto.RefreshToken.Trim(), cancellationToken);
        if (refreshToken is null)
            return new ServiceResult(false, "Refresh token topilmadi.");

        if (!refreshToken.IsActive)
            return new ServiceResult(false, "Refresh token allaqachon yaroqsiz.");

        await _refreshTokenService.RevokeAsync(refreshToken, "User logout", cancellationToken: cancellationToken);
        return new ServiceResult(true, "Sessiya yopildi.");
    }

    public async Task<ServiceResult> UpdateProfileAsync(
    string userId,
    UpdateProfileDto dto,
    CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(userId, out var parsedUserId))
            return new ServiceResult(false, "Foydalanuvchi identifikatori xato.");

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId, cancellationToken);
        if (user is null)
            return new ServiceResult(false, "Foydalanuvchi topilmadi.");

        var newUserName = dto.UserName.Trim();
        var newPhoneNumber = dto.PhoneNumber.Trim();
        var newFullName = dto.FullName.Trim();

        var userNameExists = await _dbContext.Users.AnyAsync(
            x => x.UserName == newUserName && x.Id != user.Id,
            cancellationToken);

        if (userNameExists)
            return new ServiceResult(false, "Bu username band.");

        var phoneExists = await _dbContext.Users.AnyAsync(
            x => x.PhoneNumber == newPhoneNumber && x.Id != user.Id,
            cancellationToken);

        if (phoneExists)
            return new ServiceResult(false, "Bu telefon raqam allaqachon ishlatilgan.");

        user.FullName = newFullName;
        user.UserName = newUserName;
        user.PhoneNumber = newPhoneNumber;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServiceResult(true, "Profil muvaffaqiyatli yangilandi.");
    }

    private async Task<AppUser?> FindUserByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        var value = identifier.Trim();

        return await _dbContext.Users.FirstOrDefaultAsync(
            x => x.UserName == value || x.PhoneNumber == value,
            cancellationToken);
    }
}