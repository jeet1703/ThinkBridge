using Microsoft.Extensions.Logging;
using QuotesApi.Services;
using Xunit;

namespace QuotesApi.Tests;

public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);
}

public class FakeLogger : ILogger<QuoteLogger>
{
    public List<string> LoggedMessages { get; } = new();

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LoggedMessages.Add(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

public class QuoteLoggerTests
{
    [Fact]
    public void QuoteLogger_ShouldIncludeExactFakeClockTimestamp()
    {
        // Arrange
        var fakeClock = new FakeClock();
        var fakeLogger = new FakeLogger();
        var quoteLogger = new QuoteLogger(fakeLogger, fakeClock);

        // Act
        quoteLogger.LogInformation("New quote created");

        // Assert
        var expectedTimestampString = $"[{fakeClock.UtcNow:O}]";
        Assert.Single(fakeLogger.LoggedMessages);
        Assert.StartsWith(expectedTimestampString, fakeLogger.LoggedMessages[0]);
    }
}
