using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.DTOs;
using QuotesApi.Models;
using QuotesApi.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Quotes.Tests.Integration;

public class IntegrationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public IntegrationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
        _factory.ClearDatabase();
    }

    [Fact]
    public async Task GetQuotes_DefaultPageAndSize_ShouldReturnOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/quotes?page=1&size=10");
        var content = await response.Content.ReadAsStringAsync();
        throw new Exception($"DEBUG RESPONSE: Status={response.StatusCode}, Body={content}");
    }

    [Fact]
    public async Task GetQuotes_InvalidPageAndSize_ShouldReturnValidationProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/quotes?page=0&size=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("page");
        problem.Errors.Should().ContainKey("size");
    }

    [Fact]
    public async Task PostQuote_Anonymous_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateQuoteRequest { Author = "Author", Text = "Valid Quote Text" };

        var response = await client.PostAsJsonAsync("/api/quotes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostQuote_MissingScope_ShouldReturnForbidden()
    {
        var client = _factory.CreateAuthenticatedClient("user@example.com", 1, scope: null);
        var request = new CreateQuoteRequest { Author = "Author", Text = "Valid Quote Text" };

        var response = await client.PostAsJsonAsync("/api/quotes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostQuote_ValidRequest_ShouldReturnCreatedAndDatabasePersist()
    {
        var client = _factory.CreateAuthenticatedClient("user@example.com", 1);
        var request = new CreateQuoteRequest { Author = "Author Test", Text = "Valid Quote Text" };

        var response = await client.PostAsJsonAsync("/api/quotes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Quote>();
        created.Should().NotBeNull();
        created!.Author.Should().Be("Author Test");
        created.Text.Should().Be("Valid Quote Text");
        created.CreatedByUserId.Should().Be(1);
    }

    [Fact]
    public async Task PostQuote_InvalidInput_ShouldReturnValidationProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient("user@example.com", 1);
        var request = new CreateQuoteRequest { Author = "", Text = "" };

        var response = await client.PostAsJsonAsync("/api/quotes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("author");
        problem.Errors.Should().ContainKey("text");
    }

    [Fact]
    public async Task GetQuoteById_NonExistentId_ShouldReturnNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/quotes/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Quote not found.");
    }

    [Fact]
    public async Task DeleteQuote_Anonymous_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/quotes/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteQuote_NotOwner_ShouldReturnForbidden()
    {
        var client = _factory.CreateAuthenticatedClient("owner@example.com", 1);
        var setupResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Test", Text = "Some quote content here." });
        setupResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = await setupResponse.Content.ReadFromJsonAsync<Quote>();

        var nonOwnerClient = _factory.CreateAuthenticatedClient("other@example.com", 2);

        var response = await nonOwnerClient.DeleteAsync($"/api/quotes/{quote!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteQuote_Owner_ShouldReturnNoContent()
    {
        var client = _factory.CreateAuthenticatedClient("owner@example.com", 1);
        var setupResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Test", Text = "Some quote content here." });
        var quote = await setupResponse.Content.ReadFromJsonAsync<Quote>();

        var response = await client.DeleteAsync($"/api/quotes/{quote!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("admin@example.com", "WrongPassword");

        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_TokenReuse_ShouldTriggerRevocationAndReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
        var rts = scope.ServiceProvider.GetRequiredService<RefreshTokenService>();

        var user = new User { Email = "user@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123") };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "Password123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var rawRefresh = tokens!.Refresh_Token;

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var freshTokens = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rawRefresh));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var familyCheckResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(freshTokens!.Refresh_Token));
        familyCheckResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCollection_Anonymous_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateCollectionRequest("Short Name", "owner-1");

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<User> SeedUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
        var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task PostCollection_ValidName_ShouldReturnCreated()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        var request = new CreateCollectionRequest("My Favorite Quotes Collection", "user-123");

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Collection>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("My Favorite Quotes Collection");
    }

    [Fact]
    public async Task PostCollection_InvalidName_ShouldReturnBadRequest()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        var request = new CreateCollectionRequest("ab", "user-123");

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCollection_MissingOwnerId_ShouldReturnBadRequest()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        var request = new CreateCollectionRequest("Valid Name", "");

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddCollectionItem_ValidRequest_ShouldReturnOk()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        
        // 1. Create collection
        var colResponse = await client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Collection One", "user-1"));
        colResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var colJson = await colResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(colJson);
        var collectionId = doc.RootElement.GetProperty("id").GetInt32();

        // 2. Create quote
        var qResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Author", Text = "Quote text content" });
        qResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = await qResponse.Content.ReadFromJsonAsync<Quote>();

        // 3. Add to collection
        var response = await client.PostAsJsonAsync($"/api/collections/{collectionId}/items", new AddQuoteRequest(quote!.Id));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddCollectionItem_CollectionNotFound_ShouldReturnNotFound()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        var response = await client.PostAsJsonAsync("/api/collections/9999/items", new AddQuoteRequest(1));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddCollectionItem_QuoteNotFound_ShouldReturnNotFound()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        
        // Create collection
        var colResponse = await client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Collection One", "user-1"));
        var colJson = await colResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(colJson);
        var collectionId = doc.RootElement.GetProperty("id").GetInt32();

        var response = await client.PostAsJsonAsync($"/api/collections/{collectionId}/items", new AddQuoteRequest(9999));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveCollectionItem_ValidRequest_ShouldReturnOk()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);

        // 1. Create collection
        var colResponse = await client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Collection One", "user-1"));
        var colJson = await colResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(colJson);
        var collectionId = doc.RootElement.GetProperty("id").GetInt32();

        // 2. Create quote
        var qResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Author", Text = "Quote text content" });
        var quote = await qResponse.Content.ReadFromJsonAsync<Quote>();

        // 3. Add to collection
        await client.PostAsJsonAsync($"/api/collections/{collectionId}/items", new AddQuoteRequest(quote!.Id));

        // 4. Remove from collection
        var response = await client.DeleteAsync($"/api/collections/{collectionId}/items/{quote.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveCollectionItem_CollectionNotFound_ShouldReturnNotFound()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);
        var response = await client.DeleteAsync("/api/collections/9999/items/1");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logout_ValidToken_ShouldReturnNoContent()
    {
        var client = _factory.CreateClient();
        var user = await SeedUserAsync("admin@example.com", "AdminPassword123");
        
        // Login first
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "AdminPassword123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(tokens!.Refresh_Token));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_MissingToken_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_MissingCredentials_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_MissingToken_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddCollectionItem_DuplicateQuote_ShouldReturnBadRequest()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);

        // 1. Create collection
        var colResponse = await client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Collection One", "user-1"));
        var colJson = await colResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(colJson);
        var collectionId = doc.RootElement.GetProperty("id").GetInt32();

        // 2. Create quote
        var qResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Author", Text = "Quote text content" });
        var quote = await qResponse.Content.ReadFromJsonAsync<Quote>();

        // 3. Add to collection
        var res1 = await client.PostAsJsonAsync($"/api/collections/{collectionId}/items", new AddQuoteRequest(quote!.Id));
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Add again
        var res2 = await client.PostAsJsonAsync($"/api/collections/{collectionId}/items", new AddQuoteRequest(quote!.Id));
        res2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveCollectionItem_NotFound_ShouldReturnBadRequest()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);

        // Create collection
        var colResponse = await client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Collection One", "user-1"));
        var colJson = await colResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(colJson);
        var collectionId = doc.RootElement.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/api/collections/{collectionId}/items/9999");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuoteById_ExistingId_ShouldReturnOk()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);

        var qResponse = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteRequest { Author = "Author", Text = "Quote text content" });
        var quote = await qResponse.Content.ReadFromJsonAsync<Quote>();

        var response = await client.GetAsync($"/api/quotes/{quote!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteQuote_NonExistentId_ShouldReturnNotFound()
    {
        var user = await SeedUserAsync("user@example.com", "Password123");
        var client = _factory.CreateAuthenticatedClient(user.Email, user.Id);

        var response = await client.DeleteAsync("/api/quotes/9999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CallApi_MalformedToken_ShouldFallbackToLocalJwtAndReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("different_secret_signing_key_that_is_at_least_32_bytes_long!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "QuotesApiIssuer",
            audience: "QuotesApiAudience",
            claims: new[] { new Claim(ClaimTypes.Name, "test") },
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStr);
        var response = await client.DeleteAsync("/api/quotes/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CallApi_EntraIdToken_ShouldReturnOk()
    {
        var user = await SeedUserAsync("entra-user@example.com", "Password123");
        var client = _factory.CreateEntraClient(user.Email, user.Id);

        var request = new CreateQuoteRequest { Author = "Entra Author", Text = "Entra Valid Quote Text" };
        var response = await client.PostAsJsonAsync("/api/quotes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CallApi_EntraIdToken_WrongScope_ShouldReturnForbidden()
    {
        var user = await SeedUserAsync("entra-user@example.com", "Password123");
        var client = _factory.CreateEntraClient(user.Email, user.Id, scope: "wrong.scope");

        var request = new CreateQuoteRequest { Author = "Entra Author", Text = "Entra Valid Quote Text" };
        var response = await client.PostAsJsonAsync("/api/quotes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
