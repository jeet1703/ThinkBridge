using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotesApi.DTOs;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace QuotesApi.Extensions;

public static class QuoteEndpointExtensions
{
    private static readonly ActivitySource ActivitySource = new("QuotesApi");

    public static IEndpointRouteBuilder MapQuoteEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/quotes");

        group.MapGet("/", async (
            int page,
            int size,
            IQuoteRepository repository,
            ILogger<QuoteLogger> logger,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || size < 1 || size > 100)
            {
                var errors = new Dictionary<string, string[]>();

                if (page < 1)
                    errors["page"] = ["Page must be greater than 0."];

                if (size < 1 || size > 100)
                    errors["size"] = ["Size must be between 1 and 100."];

                return Results.ValidationProblem(errors);
            }

            logger.LogInformation(
                "Fetching quotes page {Page} with size {Size}",
                page,
                size);

            using var activity = ActivitySource.StartActivity("quotes.list");
            activity?.SetTag("quotes.page", page);
            activity?.SetTag("quotes.size", size);

            var result = await repository.GetPagedAsync(
                page,
                size,
                cancellationToken);

            return Results.Ok(new
            {
                page,
                size,
                total = result.Total,
                items = result.Items
            });
        });

        group.MapPost("/", async (
            CreateQuoteRequest request,
            IQuoteRepository repository,
            ClaimsPrincipal user,
            ILogger<QuoteLogger> logger,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Author))
                errors["author"] = ["Author is required."];

            if (string.IsNullOrWhiteSpace(request.Text))
                errors["text"] = ["Text is required."];

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            int? createdByUserId = null;
            var subClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (subClaim != null && int.TryParse(subClaim, out var parsedUserId))
            {
                createdByUserId = parsedUserId;
            }

            var quote = new Quote
            {
                Author = request.Author.Trim(),
                Text = request.Text.Trim(),
                CreatedByUserId = createdByUserId
            };

            var created = await repository.AddAsync(
                quote,
                cancellationToken);

            logger.LogInformation(
                "Created quote {QuoteId}",
                created.Id);

            return Results.Created(
                $"/api/quotes/{created.Id}",
                created);
        }).RequireAuthorization("can-edit-quotes");

        group.MapGet("/{id:int}", async (
            int id,
            IQuoteRepository repository,
            ILogger<QuoteLogger> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation(
                "Getting quote {QuoteId}",
                id);

            var quote = await repository.GetByIdAsync(
                id,
                cancellationToken);

            if (quote is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Quote not found.",
                    Detail = $"Quote {id} was not found."
                });
            }

            return Results.Ok(quote);
        });

        group.MapDelete("/{id:int}", async (
            int id,
            IQuoteRepository repository,
            IAuthorizationService authorizationService,
            ClaimsPrincipal user,
            ILogger<QuoteLogger> logger,
            CancellationToken cancellationToken) =>
        {
            var quote = await repository.GetByIdAsync(id, cancellationToken);
            if (quote is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Quote not found.",
                    Detail = $"Quote {id} was not found."
                });
            }

            var authResult = await authorizationService.AuthorizeAsync(user, quote, "can-delete-own-quote");
            if (!authResult.Succeeded)
            {
                return Results.Forbid();
            }

            await repository.DeleteAsync(id, cancellationToken);

            logger.LogInformation(
                "Deleted quote {QuoteId}",
                id);

            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }
}