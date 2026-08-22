using System;
using System.Collections.Generic;

namespace QuotesApi.Features.Collections.Queries.GetCollection;

public record CollectionReadModel(
    int Id,
    string Name,
    string OwnerId,
    List<CollectionItemReadModel> Items);

public record CollectionItemReadModel(
    int QuoteId,
    string QuoteText,
    string Author,
    DateTime AddedAt);
