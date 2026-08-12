using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace QuotesApi.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly QuotesDbContext _context;

    public CollectionRepository(QuotesDbContext context)
    {
        _context = context;
    }

    public async Task<Collection?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Collections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Collection> AddAsync(Collection collection, CancellationToken cancellationToken)
    {
        await _context.Collections.AddAsync(collection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return collection;
    }

    public async Task UpdateAsync(Collection collection, CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var collection = await GetByIdAsync(id, cancellationToken);
        if (collection == null) return false;

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
