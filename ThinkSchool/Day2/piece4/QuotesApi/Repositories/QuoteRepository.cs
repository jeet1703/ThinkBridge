using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly QuotesDbContext _db;

    public QuoteRepository(QuotesDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Quote> Items, int Total)> GetPagedAsync(
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        var query = _db.Quotes
            .AsNoTracking()
            .Where(q => !q.IsDeleted)
            .OrderBy(q => q.Id);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Quote?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return _db.Quotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                q => q.Id == id && !q.IsDeleted,
                cancellationToken);
    }

    public async Task<Quote> AddAsync(
        Quote quote,
        CancellationToken cancellationToken)
    {
        _db.Quotes.Add(quote);

        await _db.SaveChangesAsync(cancellationToken);

        return quote;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var quote = await _db.Quotes
            .FirstOrDefaultAsync(
                q => q.Id == id && !q.IsDeleted,
                cancellationToken);

        if (quote is null)
            return false;

        quote.Delete();

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}