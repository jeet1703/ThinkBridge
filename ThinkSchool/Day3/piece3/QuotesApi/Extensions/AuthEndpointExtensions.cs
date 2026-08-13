using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.DTOs;
using QuotesApi.Models;
using QuotesApi.Options;
using QuotesApi.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QuotesApi.Extensions;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", async (
            LoginRequest request,
            QuotesDbContext db,
            IOptionsSnapshot<JwtOptions> jwtOptions) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email and password are required.");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var options = jwtOptions.Value;
            var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("scope", "quotes.write")
            };

            var token = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(options.AccessTokenLifetime),
                signingCredentials: creds
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessTokenString = tokenHandler.WriteToken(token);

            var rawRefreshToken = GenerateRefreshTokenString();
            var hashedRefreshToken = HashToken(rawRefreshToken);

            var refreshTokenRecord = new RefreshToken
            {
                Token = hashedRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.Add(options.RefreshTokenLifetime)
            };
            db.RefreshTokens.Add(refreshTokenRecord);
            await db.SaveChangesAsync();

            return Results.Ok(new LoginResponse(
                accessTokenString,
                rawRefreshToken,
                (int)options.AccessTokenLifetime.TotalSeconds
            ));
        });

        endpoints.MapPost("/api/auth/refresh", async (
            RefreshRequest request,
            IOptionsSnapshot<JwtOptions> jwtOptions,
            RefreshTokenService refreshTokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest("Refresh token is required.");
            }

            var newRawRefreshToken = GenerateRefreshTokenString();
            var newHashedRefreshToken = refreshTokenService.HashToken(newRawRefreshToken);

            var (success, error, user) = await refreshTokenService.RefreshTokenAsync(request.RefreshToken, newHashedRefreshToken);

            if (!success || user == null)
            {
                return Results.Unauthorized();
            }

            var options = jwtOptions.Value;
            var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("scope", "quotes.write")
            };

            var accessToken = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(options.AccessTokenLifetime),
                signingCredentials: creds
            );

            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            return Results.Ok(new LoginResponse(
                accessTokenString,
                newRawRefreshToken,
                (int)options.AccessTokenLifetime.TotalSeconds
            ));
        });

        endpoints.MapPost("/api/auth/logout", async (
            LogoutRequest request,
            QuotesDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest("Refresh token is required.");
            }

            var hashedToken = HashToken(request.RefreshToken);
            var refreshTokenRecord = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

            if (refreshTokenRecord != null && refreshTokenRecord.RevokedAt == null)
            {
                refreshTokenRecord.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        });

        return endpoints;
    }

    private static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
