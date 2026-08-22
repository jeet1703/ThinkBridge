using MediatR;
using Microsoft.AspNetCore.Http;

namespace QuotesApi.Features.Collections.Queries.GetCollection;

public record GetCollectionQuery(int CollectionId) : IRequest<IResult>;
