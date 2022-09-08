using System.Net.Http.Headers;
using System.Security.Claims;
using Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.Authentication
{
    public class ClaimHeaderAppender : RequestTransform
    {
        public override async ValueTask ApplyAsync(RequestTransformContext context)
        {
            context.ProxyRequest.Headers.Remove(HeaderNames.Cookie);
            context.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated ?? false)
            {
                AddClaim(context, DefaultHeaders.Name, ClaimTypes.Name, ClaimTypes.NameIdentifier);
                AddClaim(context, DefaultHeaders.Email, ClaimTypes.Email);
                AddClaim(context, DefaultHeaders.GivenName, ClaimTypes.GivenName, "given_name");
                AddClaim(context, DefaultHeaders.Surname, ClaimTypes.Surname, "family_name");
                var accessToken = await context.HttpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            }
        }

        private void AddClaim(RequestTransformContext context, string forwardHeaderName, params string[] claimTypes)
        {
            context.ProxyRequest.Headers.Remove(forwardHeaderName);

            foreach (var claimType in claimTypes)
            {
                var claim = context.HttpContext.User.FindFirst(e => e.Type == claimType);
                if (claim is not null)
                {
                    context.ProxyRequest.Headers.Add(forwardHeaderName, claim.Value);
                    break;
                }
            }
        }
    }
}