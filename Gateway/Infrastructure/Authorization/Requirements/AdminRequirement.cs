using Microsoft.AspNetCore.Authorization;

namespace Gateway.Infrastructure.Authorization.Requirements
{
    public class AdminRequirement : IAuthorizationRequirement
    {
        public readonly string[] Administrators;

        public AdminRequirement(string[] administrators)
        {
            Administrators = administrators;
        }
    }
}
