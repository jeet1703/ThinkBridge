using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Repositories;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuotesApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
    }
}

public class CollectionCancellationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionCancellationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Repository_ShouldThrow_WhenCancellationRequested()
    {
        var options = new DbContextOptionsBuilder<QuotesDbContext>()
            .UseInMemoryDatabase(databaseName: "CancellationTestDb")
            .Options;

        using var context = new QuotesDbContext(options);
        var repository = new CollectionRepository(context);
        var collection = new Collection("Test Collection", "owner-123");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await repository.AddAsync(collection, cts.Token);
        });
    }

    [Fact]
    public async Task Endpoint_ShouldHonorCancellation_WhenClientCancelsMidRequest()
    {
        var client = _factory.CreateClient();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await client.PostAsJsonAsync("/api/collections", new { name = "Test", ownerId = "owner-1" }, cts.Token);
        });
    }
}
