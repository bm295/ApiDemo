using System.Security.Claims;
using Cqrs.RetailerIsolation;

namespace FunctionalProgramming.Services.Authentication;

public sealed class ClaimRetailerContext(IHttpContextAccessor httpContextAccessor) : IRetailerContext
{
    public const string RetailerIdClaimType = "retailer_id";

    public Guid RetailerId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(RetailerIdClaimType);
            return Guid.TryParse(value, out var retailerId)
                ? retailerId
                : throw new UnauthorizedAccessException("The authenticated user must have a valid retailer_id claim.");
        }
    }
}
