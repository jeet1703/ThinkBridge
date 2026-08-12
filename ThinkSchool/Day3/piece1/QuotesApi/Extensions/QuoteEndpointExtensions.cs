using Microsoft.AspNetCore.Mvc;
using QuotesApi.DTOs;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class QuoteEndpointExtensions
{
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

            var quote = new Quote
            {
                Author = request.Author.Trim(),
                Text = request.Text.Trim()
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
        }).RequireAuthorization();

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
            ILogger<QuoteLogger> logger,
            CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(
                id,
                cancellationToken);

            if (!deleted)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Quote not found.",
                    Detail = $"Quote {id} was not found."
                });
            }

            logger.LogInformation(
                "Deleted quote {QuoteId}",
                id);

            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }
}