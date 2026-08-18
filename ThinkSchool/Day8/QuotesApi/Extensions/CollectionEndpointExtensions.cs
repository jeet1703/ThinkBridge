using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QuotesApi.DTOs;
using QuotesApi.Models;
using QuotesApi.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuotesApi.Extensions;

public static class CollectionEndpointExtensions
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/collections");

        group.MapPost("/", async (
            CreateCollectionRequest request,
            ICollectionRepository repository,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors["name"] = ["Name is required."];
            }
            else
            {
                string trimmed = request.Name.Trim();
                if (trimmed.Length < 3 || trimmed.Length > 80)
                {
                    errors["name"] = ["Name must be between 3 and 80 characters."];
                }
            }

            if (string.IsNullOrWhiteSpace(request.OwnerId))
            {
                errors["ownerId"] = ["Owner ID is required."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var collection = new Collection(request.Name, request.OwnerId);
            var created = await repository.AddAsync(collection, cancellationToken);
            return Results.Created($"/api/collections/{created.Id}", created);
        }).RequireAuthorization();

        group.MapPost("/{id:int}/items", async (
            int id,
            AddQuoteRequest request,
            ICollectionRepository repository,
            IQuoteRepository quoteRepository,
            CancellationToken cancellationToken) =>
        {
            var collection = await repository.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Collection not found.",
                    Detail = $"Collection {id} was not found."
                });
            }

            var quote = await quoteRepository.GetByIdAsync(request.QuoteId, cancellationToken);
            if (quote == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Quote not found.",
                    Detail = $"Quote {request.QuoteId} was not found."
                });
            }

            try
            {
                collection.AddItem(request.QuoteId);
                await repository.UpdateAsync(collection, cancellationToken);
                return Results.Ok(collection);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid operation.",
                    Detail = ex.Message
                });
            }
        }).RequireAuthorization();

        group.MapDelete("/{id:int}/items/{quoteId:int}", async (
            int id,
            int quoteId,
            ICollectionRepository repository,
            CancellationToken cancellationToken) =>
        {
            var collection = await repository.GetByIdAsync(id, cancellationToken);
            if (collection == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Collection not found.",
                    Detail = $"Collection {id} was not found."
                });
            }

            try
            {
                collection.RemoveItem(quoteId);
                await repository.UpdateAsync(collection, cancellationToken);
                return Results.Ok(collection);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid operation.",
                    Detail = ex.Message
                });
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
