using Microsoft.Extensions.Logging;

namespace QuotesApi.Services;

public class QuoteLogger
{
    private readonly ILogger<QuoteLogger> _logger;
    private readonly IClock _clock;

    public QuoteLogger(ILogger<QuoteLogger> logger, IClock clock)
    {
        _logger = logger;
        _clock = clock;
    }

    public void LogInformation(string message, params object?[] args)
    {
        var timestampedMessage = $"[{_clock.UtcNow:O}] " + message;
        _logger.LogInformation(timestampedMessage, args);
    }
}