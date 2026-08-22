using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuotesApi.Features.Collections.Queries.GetCollection;

public class GetCollectionQueryHandler : IRequestHandler<GetCollectionQuery, IResult>
{
    private readonly QuotesDbContext _context;

    public GetCollectionQueryHandler(QuotesDbContext context)
    {
        _context = context;
    }

    public async Task<IResult> Handle(GetCollectionQuery request, CancellationToken cancellationToken)
    {
        var collection = await _context.Collections
            .AsNoTracking()
            .Where(c => c.Id == request.CollectionId)
            .Select(c => new CollectionReadModel(
                c.Id,
                c.Name,
                c.OwnerId,
                c.Items.Select(i => new CollectionItemReadModel(
                    i.QuoteId,
                    _context.Quotes.Where(q => q.Id == i.QuoteId).Select(q => q.Text).FirstOrDefault() ?? "",
                    _context.Quotes.Where(q => q.Id == i.QuoteId).Select(q => q.Author).FirstOrDefault() ?? "",
                    i.AddedAt
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (collection == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Collection not found.",
                Detail = $"Collection {request.CollectionId} was not found."
            });
        }

        return Results.Ok(collection);
    }
}
