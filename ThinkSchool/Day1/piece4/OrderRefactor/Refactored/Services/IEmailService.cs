using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string email, int orderId, decimal total, CancellationToken cancellationToken);
    }
}
