using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Models;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Quotes.Tests.Integration;

public class QueryTranslationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly ITestOutputHelper _output;

    public QueryTranslationTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _factory.InitializeDatabase();
    }

    private class QuoteDto
    {
        public int Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    [Fact]
    public void Test_1_WholeEntityQuery()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        var query = context.Quotes;
        var sql = query.ToQueryString();

        _output.WriteLine("original sql is:");
        _output.WriteLine(sql);
    }

    [Fact]
    public void Test_2_ProjectedDtoQuery()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        var query = context.Quotes.Select(q => new QuoteDto
        {
            Id = q.Id,
            Author = q.Author,
            Text = q.Text
        });
        var sql = query.ToQueryString();

        _output.WriteLine("projected sql is:");
        _output.WriteLine(sql);
    }

    [Fact]
    public void Test_3_ClientEvalErrorAndFix()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();

        InvalidOperationException? caughtException = null;
        try
        {
            var query = context.Quotes.Where(q => FormatAuthor(q.Author) == "ADA LOVELACE");
            var sql = query.ToQueryString();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        _output.WriteLine("caught client-eval error:");
        _output.WriteLine(caughtException?.Message);

        Assert.NotNull(caughtException);

        var fixedQuery = context.Quotes.Where(q => q.Author.ToUpper() == "ADA LOVELACE");
        var fixedSql = fixedQuery.ToQueryString();

        _output.WriteLine("\nfixed sql is:");
        _output.WriteLine(fixedSql);
    }

    private static string FormatAuthor(string author) => author.Trim().ToUpper();
}
