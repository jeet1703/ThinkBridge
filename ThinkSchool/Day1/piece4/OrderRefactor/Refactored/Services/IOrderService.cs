using OrderRefactor.Refactored.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    }
}
