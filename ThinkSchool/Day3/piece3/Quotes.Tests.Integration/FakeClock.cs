using QuotesApi.Services;
using System;

namespace Quotes.Tests.Integration;

public class FakeClock : IClock
{
    public Func<DateTime>? UtcNowFunc { get; set; }
    
    public DateTime UtcNow 
    { 
        get => UtcNowFunc != null ? UtcNowFunc() : DateTime.UtcNow; 
        set => UtcNowFunc = () => value; 
    }
}
