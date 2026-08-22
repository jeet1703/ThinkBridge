using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuotesApi.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuotesApi.Features.Collections.Commands.AddQuoteToCollection;

public class AddQuoteToCollectionCommandHandler : IRequestHandler<AddQuoteToCollectionCommand, IResult>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IQuoteRepository _quoteRepository;

    public AddQuoteToCollectionCommandHandler(
        ICollectionRepository collectionRepository,
        IQuoteRepository quoteRepository)
    {
        _collectionRepository = collectionRepository;
        _quoteRepository = quoteRepository;
    }

    public async Task<IResult> Handle(AddQuoteToCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdAsync(request.CollectionId, cancellationToken);
        if (collection == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Collection not found.",
                Detail = $"Collection {request.CollectionId} was not found."
            });
        }

        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId, cancellationToken);
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
            await _collectionRepository.UpdateAsync(collection, cancellationToken);
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
    }
}
