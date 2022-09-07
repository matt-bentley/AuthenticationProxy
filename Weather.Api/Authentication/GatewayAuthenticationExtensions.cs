using Microsoft.AspNetCore.Authentication;

namespace Weather.Api.Authentication
{
    public static class GatewayAuthenticationExtensions
    {
        public static AuthenticationBuilder AddGateway(this AuthenticationBuilder builder)
        {
            return builder.AddScheme<GatewayAuthenticationOptions, GatewayAuthenticationHandler>(GatewayAuthenticationDefaults.AuthenticationScheme, options => { });
        }
    }
}
