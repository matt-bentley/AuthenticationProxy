using Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Weather.Api.Authentication
{
    public class GatewayAuthenticationHandler : AuthenticationHandler<GatewayAuthenticationOptions>
    {
        public GatewayAuthenticationHandler(
            IOptionsMonitor<GatewayAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
            {
                return Task.FromResult(AuthenticateResult.Fail("Bearer token not found"));
            }

            if (!Request.Headers.TryGetValue(DefaultHeaders.Email, out var email))
            {
                return Task.FromResult(AuthenticateResult.Fail("Forwarded identity headers not found"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email.ToString()),
                new Claim(ClaimTypes.Email, email.ToString()),
                new Claim(ClaimTypes.Name, Request.Headers[DefaultHeaders.Name]),
                new Claim(ClaimTypes.GivenName, Request.Headers[DefaultHeaders.GivenName]),
                new Claim(ClaimTypes.Surname, Request.Headers[DefaultHeaders.Surname])
            };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(GatewayAuthenticationHandler), ClaimTypes.Email, ClaimTypes.Role);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
