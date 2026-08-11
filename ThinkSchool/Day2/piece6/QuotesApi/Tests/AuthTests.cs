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
        // Arrange
        var client = _factory.CreateClient();
        
        // 1. Initial Login to obtain Token Pair 1 (Access1 + Refresh1)
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@example.com", "Password123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var tokens1 = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(tokens1);
        var rawRefresh1 = tokens1.Refresh_Token;

        // 2. Perform first refresh using Refresh1 -> Generates Token Pair 2 (Access2 + Refresh2)
        var refreshResponse1 = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh1));
        Assert.Equal(HttpStatusCode.OK, refreshResponse1.StatusCode);
        var tokens2 = await refreshResponse1.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(tokens2);
        var rawRefresh2 = tokens2.Refresh_Token;

        // 3. Attempt to REUSE Refresh1 (already rotated)
        // This must trigger reuse detection and revoke the entire token family (including active Refresh2)
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh1));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // 4. Attempt to use the otherwise valid Refresh2 (grandchild token)
        // This must now return 401 Unauthorized because its family was fully revoked by step 3
        var familyCheckResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh2));
        Assert.Equal(HttpStatusCode.Unauthorized, familyCheckResponse.StatusCode);
    }
}
