using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IHttpClientFactory clientFactory, ILogger<AnalyticsService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task TrackOrderAttemptAsync(string payload, CancellationToken cancellationToken)
        {
            try
            {
                var client = _clientFactory.CreateClient("AnalyticsClient");
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync("http://internal-analytics.local/track", content, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Analytics tracking failed.");
            }
        }
    }
}
