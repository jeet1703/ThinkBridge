using Microsoft.Extensions.Logging;
using System;

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

    public void LogQuoteCreation(int quoteId, string author)
    {
        _logger.LogInformation(
            "Quote {QuoteId} by {Author} was created at {Time}",
            quoteId,
            author,
            _clock.UtcNow);
    }
}