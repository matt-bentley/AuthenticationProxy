using System.Net.Http.Headers;
using System.Security.Claims;
using Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace NMica.SecurityProxy.Middleware.Transforms;

public class ClaimHeaderAppender : RequestTransform
{
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        context.ProxyRequest.Headers.Remove(HeaderNames.Cookie);
        context.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated ?? false)
        {
            AddClaim(context, ClaimTypes.Name, DefaultHeaders.Name);
            AddClaim(context, ClaimTypes.Email, DefaultHeaders.Email);
            AddClaim(context, ClaimTypes.GivenName, DefaultHeaders.GivenName);
            AddClaim(context, "given_name", DefaultHeaders.GivenName);
            AddClaim(context, ClaimTypes.Surname, DefaultHeaders.Surname);
            AddClaim(context, "family_name", DefaultHeaders.Surname);
            var accessToken = await context.HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }
    }

    private void AddClaim(RequestTransformContext context, string claimType, string forwardHeaderName)
    {
        context.ProxyRequest.Headers.Remove(forwardHeaderName);
        var claim = context.HttpContext.User.FindFirst(e => e.Type == claimType);
        if(claim is not null)
        {
            context.ProxyRequest.Headers.Add(forwardHeaderName, claim.Value);
        }
    }
}