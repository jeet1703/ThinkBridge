using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Authorization;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;
using QuotesApi.Validators;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace QuotesApi.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("QuotesDb")
            ?? "Data Source=quotes.db";

        services.AddDbContext<QuotesDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<QuoteLogger>();
        services.AddScoped<CreateQuoteRequestValidator>();
        services.AddScoped<RefreshTokenService>();

        var jwtSettings = configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "JwtPolicy";
            options.DefaultChallengeScheme = "JwtPolicy";
        })
        .AddPolicyScheme("JwtPolicy", "JwtPolicy", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                string auth = context.Request.Headers.Authorization.ToString();
                if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return "LocalJwt";
                }

                var tokenStr = auth.Substring("Bearer ".Length).Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(tokenStr))
                    {
                        var jwt = handler.ReadJwtToken(tokenStr);
                        if (jwt.Issuer != null && jwt.Issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
                        {
                            return "EntraId";
                        }
                    }
                }
                catch
                {
                }

                return "LocalJwt";
            };
        })
        .AddJwtBearer("LocalJwt", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddJwtBearer("EntraId", options =>
        {
            var entraSettings = configuration.GetSection("EntraId");
            options.Authority = $"https://login.microsoftonline.com/{entraSettings["TenantId"]}/v2.0";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false,
                ValidAudience = entraSettings["Audience"]
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("can-edit-quotes", policy =>
                policy.RequireClaim("scope", "quotes.write"));

            options.AddPolicy("can-delete-own-quote", policy =>
                policy.Requirements.Add(new SameOwnerRequirement()));
        });

        services.AddScoped<IAuthorizationHandler, SameOwnerAuthorizationHandler>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuotesDbContext>();

        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var admin = new User
            {
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}