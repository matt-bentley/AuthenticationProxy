using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gateway.Infrastructure.Authorization.Requirements
{
    public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
    {
        private readonly ILogger<AdminRequirementHandler> _logger;

        public AdminRequirementHandler(ILogger<AdminRequirementHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            var emailClaim = context.User.FindFirst(e => e.Type == ClaimTypes.Email);
            if (context.User.Identity.IsAuthenticated && emailClaim != null)
            {
                var email = emailClaim.Value;
                if(requirement.Administrators.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User is not an Administrator: {email}", email);
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
}
