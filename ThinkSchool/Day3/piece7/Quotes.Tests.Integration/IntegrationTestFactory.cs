using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Services;
using Testcontainers.MsSql;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Quotes.Tests.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeClock Clock { get; } = new();

    Task IAsyncLifetime.InitializeAsync() => _dbContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var connectionString = _dbContainer.GetConnectionString();

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<QuotesDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<QuotesDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            var clockDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IClock));
            if (clockDescriptor != null)
            {
                services.Remove(clockDescriptor);
            }
            services.AddSingleton<IClock>(Clock);
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
        db.Database.Migrate();
    }

    public void ClearDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
        db.RefreshTokens.ExecuteDelete();
        db.Collections.ExecuteDelete();
        db.Quotes.ExecuteDelete();
        db.Users.ExecuteDelete();
    }

    public HttpClient CreateAuthenticatedClient(string email, int userId, string? scope = "quotes.write")
    {
        var client = CreateClient();
        var token = GenerateToken(email, userId, scope);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateToken(string email, int userId, string? scope)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this_is_a_very_secret_signing_key_that_is_at_least_32_bytes_long_123!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (scope != null)
        {
            claims.Add(new Claim("scope", scope));
        }

        var token = new JwtSecurityToken(
            issuer: "QuotesApiIssuer",
            audience: "QuotesApiAudience",
            claims: claims,
            expires: Clock.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
