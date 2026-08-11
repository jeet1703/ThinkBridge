using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.DTOs;
using QuotesApi.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace QuotesApi.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_TokenReuse_ShouldRevokeEntireChainAndForceReAuth()
    {
        var client = _factory.CreateClient();
        
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@example.com", "Password123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var tokens1 = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(tokens1);
        var rawRefresh1 = tokens1.Refresh_Token;

        var refreshResponse1 = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh1));
        Assert.Equal(HttpStatusCode.OK, refreshResponse1.StatusCode);
        var tokens2 = await refreshResponse1.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(tokens2);
        var rawRefresh2 = tokens2.Refresh_Token;

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh1));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var familyCheckResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh2));
        Assert.Equal(HttpStatusCode.Unauthorized, familyCheckResponse.StatusCode);
    }
}
