using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text;

namespace Quotes.Tests.Integration;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    public FakeClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

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
                options.UseSqlite(_connection);
            });

            var clockDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IClock));
            if (clockDescriptor != null)
            {
                services.Remove(clockDescriptor);
            }
            services.AddSingleton<IClock>(Clock);

            services.PostConfigure<JwtBearerOptions>("EntraId", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
                };
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration
                    {
                        Issuer = "https://login.microsoftonline.com/tenant-id/v2.0"
                    });
            });
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
        db.Database.ExecuteSqlRaw("DELETE FROM Quotes");
        db.Database.ExecuteSqlRaw("DELETE FROM Users");
        db.Database.ExecuteSqlRaw("DELETE FROM RefreshTokens");
    }

    public HttpClient CreateEntraClient(string email, int userId, string? scope = "quotes.write")
    {
        var client = CreateClient();
        var token = GenerateEntraToken(email, userId, scope);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string GenerateEntraToken(string email, int userId, string? scope)
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
            issuer: "https://login.microsoftonline.com/tenant-id/v2.0",
            audience: "QuotesApiAudience",
            claims: claims,
            expires: Clock.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
