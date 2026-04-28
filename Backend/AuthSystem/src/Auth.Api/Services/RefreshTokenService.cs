using Auth.Api.Data;
using Auth.Api.Models.Identity;
using Auth.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Auth.Api.Services;

public class RefreshTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RefreshToken> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateSecureToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken refreshToken, string reason, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = reason;
        refreshToken.ReplacedByToken = replacedByToken;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken> RotateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var newToken = new RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = GenerateSecureToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = "Rotated";
        refreshToken.ReplacedByToken = newToken.Token;

        _dbContext.RefreshTokens.Add(newToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newToken;
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);

        return token
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}