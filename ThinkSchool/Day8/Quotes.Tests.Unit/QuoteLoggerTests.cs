using Microsoft.Extensions.Logging;
using NSubstitute;
using QuotesApi.Services;
using System;
using Xunit;

namespace Quotes.Tests.Unit;

public class QuoteLoggerTests
{
    private readonly ILogger<QuoteLogger> _logger = Substitute.For<ILogger<QuoteLogger>>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly QuoteLogger _sut;

    public QuoteLoggerTests()
    {
        _sut = new QuoteLogger(_logger, _clock);
    }

    [Fact]
    public void LogQuoteCreation_ValidArguments_ShouldInvokeClockAndLogCorrectMessage()
    {
        var quoteId = 12;
        var author = "Steve Jobs";
        var mockTime = new DateTime(2026, 8, 12, 10, 30, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(mockTime);

        _sut.LogQuoteCreation(quoteId, author);

        _ = _clock.Received(1).UtcNow;

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains($"Quote {quoteId} by {author} was created at")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
