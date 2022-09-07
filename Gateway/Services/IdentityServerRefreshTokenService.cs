using Gateway.Services.Abstract;
using Gateway.Settings;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Gateway.Services
{
    public class IdentityServerRefreshTokenService : RefreshTokenService
    {
        public IdentityServerRefreshTokenService(IOptions<IdentityServerSettings> options, HttpClient client) : base(options, client)
        {

        }

        public override async Task<TokenResponse> RefreshAsync(string refreshToken)
        {
            return await Client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = $"{Options.Value.Authority}/token",
                ClientId = Options.Value.ClientId,
                ClientSecret = Options.Value.ClientSecret,
                RefreshToken = refreshToken
            }).ConfigureAwait(false);
        }
    }
}
