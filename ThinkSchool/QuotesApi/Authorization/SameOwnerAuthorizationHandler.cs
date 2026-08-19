using Microsoft.AspNetCore.Authorization;
using QuotesApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace QuotesApi.Authorization;

public class SameOwnerAuthorizationHandler : AuthorizationHandler<SameOwnerRequirement, Quote>
{
    private readonly ILogger<SameOwnerAuthorizationHandler> _logger;

    public SameOwnerAuthorizationHandler(ILogger<SameOwnerAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOwnerRequirement requirement,
        Quote resource)
    {
        var userIdClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId) && resource.CreatedByUserId == userId)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} denied ownership of quote {QuoteId}",
                userIdClaim,
                resource.Id);
        }

        return Task.CompletedTask;
    }
}
