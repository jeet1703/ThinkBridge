using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendOrderConfirmationAsync(string email, int orderId, decimal total, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending email to {Email} for order {OrderId}", email, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email to {Email}", email);
            }
            return Task.CompletedTask;
        }
    }
}
