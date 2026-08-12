using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Quotes.Tests.Unit;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly QuotesDbContext _db;
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<RefreshTokenService> _logger = Substitute.For<ILogger<RefreshTokenService>>();
    private readonly RefreshTokenService _sut;
    private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuotesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new QuotesDbContext(options);
        _clock.UtcNow.Returns(_now);
        _sut = new RefreshTokenService(_db, _clock, _logger);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RefreshTokenAsync_NullOrEmptyToken_ShouldReturnError(string? invalidToken)
    {
        var (success, error, user) = await _sut.RefreshTokenAsync(invalidToken!, "new-hash");

        success.Should().BeFalse();
        error.Should().Be("Refresh token is required.");
        user.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenNotFoundInDb_ShouldReturnError()
    {
        var (success, error, user) = await _sut.RefreshTokenAsync("non-existent-token", "new-hash");

        success.Should().BeFalse();
        error.Should().Be("Invalid refresh token.");
        user.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenExpired_ShouldReturnError()
    {
        var rawToken = "my-expired-token";
        var hashedToken = _sut.HashToken(rawToken);

        var tokenRecord = new RefreshToken
        {
            Token = hashedToken,
            UserId = 1,
            ExpiresAt = _now.AddMinutes(-5)
        };
        _db.RefreshTokens.Add(tokenRecord);
        await _db.SaveChangesAsync();

        var (success, error, user) = await _sut.RefreshTokenAsync(rawToken, "new-hash");

        success.Should().BeFalse();
        error.Should().Be("Refresh token expired.");
        user.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ShouldReturnError()
    {
        var rawToken = "my-token";
        var hashedToken = _sut.HashToken(rawToken);

        var tokenRecord = new RefreshToken
        {
            Token = hashedToken,
            UserId = 9999,
            ExpiresAt = _now.AddHours(1)
        };
        _db.RefreshTokens.Add(tokenRecord);
        await _db.SaveChangesAsync();

        var (success, error, user) = await _sut.RefreshTokenAsync(rawToken, "new-hash");

        success.Should().BeFalse();
        error.Should().Be("User not found.");
        user.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ShouldRevokeOldTokenAndCreateNewOne()
    {
        var rawToken = "active-token";
        var hashedToken = _sut.HashToken(rawToken);
        var testUser = new User { Id = 1, Email = "test@example.com", PasswordHash = "hash" };
        _db.Users.Add(testUser);

        var tokenRecord = new RefreshToken
        {
            Token = hashedToken,
            UserId = testUser.Id,
            ExpiresAt = _now.AddHours(1)
        };
        _db.RefreshTokens.Add(tokenRecord);
        await _db.SaveChangesAsync();

        var newHashed = _sut.HashToken("new-token");

        var (success, error, user) = await _sut.RefreshTokenAsync(rawToken, newHashed);

        success.Should().BeTrue();
        error.Should().BeNull();
        user.Should().NotBeNull();
        user!.Id.Should().Be(testUser.Id);

        var updatedOldToken = await _db.RefreshTokens.FirstAsync(t => t.Token == hashedToken);
        updatedOldToken.RevokedAt.Should().Be(_now);
        updatedOldToken.ReplacedByToken.Should().Be(newHashed);

        var newTokenRecord = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == newHashed);
        newTokenRecord.Should().NotBeNull();
        newTokenRecord!.UserId.Should().Be(testUser.Id);
        newTokenRecord.ExpiresAt.Should().Be(_now.AddDays(7));
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenAlreadyRevoked_ShouldTriggerReuseDetectionAndRevokeFamily()
    {
        var rawToken = "already-revoked-token";
        var hashedToken = _sut.HashToken(rawToken);
        var testUser = new User { Id = 2, Email = "test2@example.com", PasswordHash = "hash" };
        _db.Users.Add(testUser);

        var revokedToken = new RefreshToken
        {
            Token = hashedToken,
            UserId = testUser.Id,
            ExpiresAt = _now.AddHours(1),
            RevokedAt = _now.AddMinutes(-10),
            ReplacedByToken = "some-replaced-hash"
        };
        _db.RefreshTokens.Add(revokedToken);

        var activeToken1 = new RefreshToken
        {
            Token = _sut.HashToken("active-family-1"),
            UserId = testUser.Id,
            ExpiresAt = _now.AddHours(2)
        };
        var activeToken2 = new RefreshToken
        {
            Token = _sut.HashToken("active-family-2"),
            UserId = testUser.Id,
            ExpiresAt = _now.AddHours(3)
        };
        _db.RefreshTokens.AddRange(activeToken1, activeToken2);
        await _db.SaveChangesAsync();

        var (success, error, user) = await _sut.RefreshTokenAsync(rawToken, "new-hash");

        success.Should().BeFalse();
        error.Should().Be("Token reuse detected.");
        user.Should().BeNull();

        var family1 = await _db.RefreshTokens.FirstAsync(t => t.Token == activeToken1.Token);
        var family2 = await _db.RefreshTokens.FirstAsync(t => t.Token == activeToken2.Token);
        family1.RevokedAt.Should().Be(_now);
        family2.RevokedAt.Should().Be(_now);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("SECURITY ALERT: Reuse of refresh token detected")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
