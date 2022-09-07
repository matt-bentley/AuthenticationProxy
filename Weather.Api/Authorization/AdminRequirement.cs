using Microsoft.AspNetCore.Authorization;

namespace Weather.Api.Authorization
{
    public class AdminRequirement : IAuthorizationRequirement
    {
        public readonly IReadOnlyList<string> Administrators;

        public AdminRequirement(params string[] administrators)
        {
            Administrators = administrators;
        }
    }
}
