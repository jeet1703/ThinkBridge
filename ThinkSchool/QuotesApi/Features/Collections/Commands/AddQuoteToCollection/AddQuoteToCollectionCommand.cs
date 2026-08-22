using MediatR;
using Microsoft.AspNetCore.Http;

namespace QuotesApi.Features.Collections.Commands.AddQuoteToCollection;

public record AddQuoteToCollectionCommand(int CollectionId, int QuoteId) : IRequest<IResult>;
