using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuotesApi.Data;
using QuotesApi.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QuotesApi.Services;

public class RefreshTokenService
{
    private readonly QuotesDbContext _db;
    private readonly IClock _clock;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(QuotesDbContext db, IClock clock, ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, User? User)> RefreshTokenAsync(string rawRefreshToken, string newHashedRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return (false, "Refresh token is required.", null);
        }

        var hashedToken = HashToken(rawRefreshToken);
        var refreshTokenRecord = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshTokenRecord == null)
        {
            return (false, "Invalid refresh token.", null);
        }

        // Reuse detection
        if (refreshTokenRecord.ReplacedByToken != null || refreshTokenRecord.RevokedAt != null)
        {
            _logger.LogWarning("SECURITY ALERT: Reuse of refresh token detected for UserId {UserId}. Revoking all active tokens.", refreshTokenRecord.UserId);

            var activeUserTokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == refreshTokenRecord.UserId && rt.RevokedAt == null && rt.ExpiresAt > _clock.UtcNow)
                .ToListAsync();

            foreach (var t in activeUserTokens)
            {
                t.RevokedAt = _clock.UtcNow;
            }
            await _db.SaveChangesAsync();

            return (false, "Token reuse detected.", null);
        }

        // Expiry check
        if (refreshTokenRecord.ExpiresAt <= _clock.UtcNow)
        {
            return (false, "Refresh token expired.", null);
        }

        var user = await _db.Users.FindAsync(refreshTokenRecord.UserId);
        if (user == null)
        {
            return (false, "User not found.", null);
        }

        // Revoke current token and link new token
        refreshTokenRecord.RevokedAt = _clock.UtcNow;
        refreshTokenRecord.ReplacedByToken = newHashedRefreshToken;

        var newRefreshTokenRecord = new RefreshToken
        {
            Token = newHashedRefreshToken,
            UserId = user.Id,
            ExpiresAt = _clock.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(newRefreshTokenRecord);
        await _db.SaveChangesAsync();

        return (true, null, user);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
