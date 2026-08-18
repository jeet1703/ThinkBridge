using QuotesApi.Services;
using System;

namespace Quotes.Tests.Integration;

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
