using Gateway.Services.Abstract;
using Gateway.Settings;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace Gateway.Services
{
    public class OktaRefreshTokenService : RefreshTokenService
    {
        private readonly AuthenticationHeaderValue _authenticationHeader;

        public OktaRefreshTokenService(IOptions<IdentityServerSettings> options, HttpClient client) : base(options, client)
        {
            var authenticationString = $"{Options.Value.ClientId}:{Options.Value.ClientSecret}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            _authenticationHeader = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString); 
        }

        public override async Task<TokenResponse> RefreshAsync(string refreshToken)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Options.Value.Authority}/v1/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {"grant_type", "refresh_token"},
                        {"refresh_token", refreshToken},
                        {"scope", "offline_access openid"},
                        {"redirect_uri", Options.Value.RedirectUri}
                    })
            };
            requestMessage.Headers.Authorization = _authenticationHeader;

            var response = await Client.SendAsync(requestMessage).ConfigureAwait(false);
            return await TokenResponse.FromHttpResponseAsync<TokenResponse>(response).ConfigureAwait(false);
        }
    }
}
