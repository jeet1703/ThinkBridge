using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public interface IAnalyticsService
    {
        Task TrackOrderAttemptAsync(string payload, CancellationToken cancellationToken);
    }
}
