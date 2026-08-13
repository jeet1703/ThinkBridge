using QuotesApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace QuotesApi.Repositories;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Collection> AddAsync(Collection collection, CancellationToken cancellationToken);
    Task UpdateAsync(Collection collection, CancellationToken cancellationToken);
}
