using OrderRefactor.Refactored.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Repositories
{
    public interface IOrderRepository
    {
        Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
        Task SaveOrderAsync(Order order, CancellationToken cancellationToken);
        Task UpdateProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken);
    }
}
