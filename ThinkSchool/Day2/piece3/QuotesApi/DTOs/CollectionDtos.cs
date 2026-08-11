using System.ComponentModel.DataAnnotations;

namespace QuotesApi.DTOs;

public record CreateCollectionRequest(
    [Required] [StringLength(80, MinimumLength = 3)] string Name,
    [Required] string OwnerId
);

public record AddQuoteRequest(
    [Required] int QuoteId
);
