using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Models;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Quotes.Tests.Integration;

public class ChangeTrackerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly ITestOutputHelper _output;

    public ChangeTrackerTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _factory.InitializeDatabase();
        _factory.ClearDatabase();
    }

    [Fact]
    public void Test_1_IdentityResolution()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        var quote = Quote.Create("Ada Lovelace", "This is a quote to test identity resolution.");
        context.Quotes.Add(quote);
        context.SaveChanges();

        var q1 = context.Quotes.First(q => q.Id == quote.Id);
        var q2 = context.Quotes.First(q => q.Id == quote.Id);

        _output.WriteLine("With tracking enabled:");
        _output.WriteLine($"q1 HashCode: {q1.GetHashCode()}");
        _output.WriteLine($"q2 HashCode: {q2.GetHashCode()}");
        _output.WriteLine($"Are references equal? {object.ReferenceEquals(q1, q2)}");
        
        Assert.Same(q1, q2);

        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<QuotesDbContext>();
        
        var q3 = context2.Quotes.AsNoTracking().First(q => q.Id == quote.Id);
        var q4 = context2.Quotes.AsNoTracking().First(q => q.Id == quote.Id);

        _output.WriteLine("\nWith AsNoTracking() enabled:");
        _output.WriteLine($"q3 HashCode: {q3.GetHashCode()}");
        _output.WriteLine($"q4 HashCode: {q4.GetHashCode()}");
        _output.WriteLine($"Are references equal? {object.ReferenceEquals(q3, q4)}");

        Assert.NotSame(q3, q4);
    }

    [Fact]
    public void Test_2_TrackingVsNot()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        var quote = Quote.Create("Alan Turing", "This is a quote to test tracking modifications.");
        context.Quotes.Add(quote);
        context.SaveChanges();

        var qTracked = context.Quotes.First(q => q.Id == quote.Id);
        qTracked.Author = "Alan T.";
        context.SaveChanges();

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<QuotesDbContext>();
            var savedQuote = verifyDb.Quotes.First(q => q.Id == quote.Id);
            _output.WriteLine($"Tracked update saved author as: '{savedQuote.Author}'");
            Assert.Equal("Alan T.", savedQuote.Author);
        }

        using var scope2 = _factory.Services.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<QuotesDbContext>();

        var qUntracked = context2.Quotes.AsNoTracking().First(q => q.Id == quote.Id);
        qUntracked.Author = "Turing Alan";
        context2.SaveChanges();

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<QuotesDbContext>();
            var savedQuote = verifyDb.Quotes.First(q => q.Id == quote.Id);
            _output.WriteLine($"Untracked update author is still: '{savedQuote.Author}'");
            Assert.Equal("Alan T.", savedQuote.Author);
        }
    }

    [Fact]
    public void Test_3_BenchmarkAsNoTracking()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        _output.WriteLine("Seeding 10,000 quotes in database... (Please wait)");
        var quotes = Enumerable.Range(1, 10000).Select(i => 
            Quote.Create("Author_" + (i % 100), $"This is quote text number {i} to benchmark change tracker performance.")
        ).ToList();

        context.Quotes.AddRange(quotes);
        context.SaveChanges();
        _output.WriteLine("Seeding complete.");

        context.ChangeTracker.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long startAllocatedTracked = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        var trackedList = context.Quotes.ToList();
        sw.Stop();
        long endAllocatedTracked = GC.GetAllocatedBytesForCurrentThread();

        long timeTrackedMs = sw.ElapsedMilliseconds;
        long bytesAllocatedTracked = endAllocatedTracked - startAllocatedTracked;

        context.ChangeTracker.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long startAllocatedNoTrack = GC.GetAllocatedBytesForCurrentThread();
        sw = Stopwatch.StartNew();
        var noTrackingList = context.Quotes.AsNoTracking().ToList();
        sw.Stop();
        long endAllocatedNoTrack = GC.GetAllocatedBytesForCurrentThread();

        long timeNoTrackMs = sw.ElapsedMilliseconds;
        long bytesAllocatedNoTrack = endAllocatedNoTrack - startAllocatedNoTrack;

        double timeRatio = (double)timeTrackedMs / (timeNoTrackMs > 0 ? timeNoTrackMs : 1);
        double allocRatio = (double)bytesAllocatedTracked / (bytesAllocatedNoTrack > 0 ? bytesAllocatedNoTrack : 1);

        _output.WriteLine("\n================ BENCHMARK RESULTS (10,000 ROWS) ================");
        _output.WriteLine($"Tracked Query      : {timeTrackedMs} ms | Allocations: {bytesAllocatedTracked / 1024.0 / 1024.0:F2} MB ({bytesAllocatedTracked} bytes)");
        _output.WriteLine($"AsNoTracking Query : {timeNoTrackMs} ms | Allocations: {bytesAllocatedNoTrack / 1024.0 / 1024.0:F2} MB ({bytesAllocatedNoTrack} bytes)");
        _output.WriteLine($"-----------------------------------------------------------------");
        _output.WriteLine($"Performance Win    : AsNoTracking is {timeRatio:F2}x faster");
        _output.WriteLine($"Memory Win         : AsNoTracking allocated {allocRatio:F2}x less memory");
        _output.WriteLine("=================================================================\n");

        Assert.Equal(10000, trackedList.Count);
        Assert.Equal(10000, noTrackingList.Count);
    }
}
