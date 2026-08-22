using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using Dapper;
using System;
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
        var connection = _context.Database.GetDbConnection();

        const string sql = @"
            SELECT 
                c.Id, c.Name, c.OwnerId,
                ci.QuoteId, q.Text AS QuoteText, q.Author, ci.AddedAt
            FROM Collections c
            LEFT JOIN CollectionItem ci ON c.Id = ci.CollectionId
            LEFT JOIN Quotes q ON ci.QuoteId = q.Id
            WHERE c.Id = @CollectionId";

        var rows = (await connection.QueryAsync<(int Id, string Name, string OwnerId, int? QuoteId, string QuoteText, string Author, DateTime? AddedAt)>(
            new CommandDefinition(sql, new { CollectionId = request.CollectionId }, cancellationToken: cancellationToken)
        )).ToList();

        if (!rows.Any())
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Collection not found.",
                Detail = $"Collection {request.CollectionId} was not found."
            });
        }

        var firstRow = rows.First();
        var items = rows
            .Where(r => r.QuoteId.HasValue)
            .Select(r => new CollectionItemReadModel(
                r.QuoteId!.Value,
                r.QuoteText ?? "",
                r.Author ?? "",
                r.AddedAt!.Value
            ))
            .ToList();

        var readModel = new CollectionReadModel(
            firstRow.Id,
            firstRow.Name,
            firstRow.OwnerId,
            items
        );

        return Results.Ok(readModel);
    }
}
