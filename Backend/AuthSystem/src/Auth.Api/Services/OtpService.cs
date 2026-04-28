using Auth.Api.Data;
using Auth.Api.Models;
using Auth.Api.Models.Identity;
using Auth.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Services;

public class OtpService
{
    private readonly AppDbContext _dbContext;
    private readonly OtpOptions _otpOptions;

    public OtpService(AppDbContext dbContext, IOptions<OtpOptions> otpOptions)
    {
        _dbContext = dbContext;
        _otpOptions = otpOptions.Value;
    }

    public async Task<(string PlainCode, VerificationCode Entity)> CreateLoginCodeAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var activeCodes = await _dbContext.VerificationCodes
            .Where(x => x.UserId == user.Id && !x.IsUsed && x.Purpose == "Login")
            .ToListAsync(cancellationToken);

        foreach (var code in activeCodes)
        {
            code.IsUsed = true;
            code.UsedAt = DateTime.UtcNow;
        }

        var plainCode = GenerateNumericCode(_otpOptions.Length);

        var entity = new VerificationCode
        {
            UserId = user.Id,
            CodeHash = ComputeSha256(plainCode),
            Purpose = "Login",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otpOptions.CodeExpiryMinutes)
        };

        _dbContext.VerificationCodes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (plainCode, entity);
    }

    public async Task<bool> VerifyLoginCodeAsync(AppUser user, string plainCode, CancellationToken cancellationToken = default)
    {
        var codeEntity = await _dbContext.VerificationCodes
            .Where(x => x.UserId == user.Id && x.Purpose == "Login" && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (codeEntity is null)
            return false;

        if (codeEntity.ExpiresAt <= DateTime.UtcNow)
            return false;

        codeEntity.AttemptCount++;

        if (codeEntity.AttemptCount > _otpOptions.MaxAttempts)
        {
            codeEntity.IsUsed = true;
            codeEntity.UsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        var incomingHash = ComputeSha256(plainCode);
        var matched = codeEntity.CodeHash == incomingHash;

        if (!matched)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        codeEntity.IsUsed = true;
        codeEntity.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public string GenerateLinkCode(int length = 8)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[length];

        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(chars.Length);
            buffer[i] = chars[index];
        }

        return new string(buffer);
    }

    private static string GenerateNumericCode(int length)
    {
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}