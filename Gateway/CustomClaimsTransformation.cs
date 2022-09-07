using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Gateway
{
    public class CustomClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var newClaims = new List<Claim>(principal.Claims);
            var nameClaim = principal.FindFirst("name");
            if (nameClaim != null)
            {
                newClaims.Remove(nameClaim);
                newClaims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
            }
            principal = new ClaimsPrincipal(new ClaimsIdentity(newClaims, principal!.Identity!.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role));
            return Task.FromResult(principal);
        }
    }
}
