using Microsoft.AspNetCore.Authorization;

namespace Weather.Api.Authorization
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
            var user = context.User.Identity.Name;
            if(string.IsNullOrEmpty(user))
            {
                _logger.LogWarning("Forwarded identity headers not found");
                context.Fail();
            }
            var isAdmin = requirement.Administrators.Any(e => e.Equals(user, StringComparison.OrdinalIgnoreCase));
            if (isAdmin)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("{user} is not an admin", user);
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
}
