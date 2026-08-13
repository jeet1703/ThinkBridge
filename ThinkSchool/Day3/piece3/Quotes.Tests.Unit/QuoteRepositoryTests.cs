using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Quotes.Tests.Unit;

public class QuoteRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_NonExistentQuote_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<QuotesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new QuotesDbContext(options);
        var repository = new QuoteRepository(context);

        var result = await repository.DeleteAsync(9999, CancellationToken.None);

        result.Should().BeFalse();
    }
}
