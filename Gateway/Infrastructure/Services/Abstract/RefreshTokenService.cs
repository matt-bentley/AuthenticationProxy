using Gateway.Interfaces.Services;
using Gateway.Settings;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Gateway.Services.Abstract
{
    public abstract class RefreshTokenService : IRefreshTokenService
    {
        protected readonly IOptions<IdentityServerSettings> Options;
        protected readonly HttpClient Client;

        public RefreshTokenService(IOptions<IdentityServerSettings> options,
            HttpClient client)
        {
            Options = options;
            Client = client;
        }

        public abstract Task<TokenResponse> RefreshAsync(string refreshToken);
    }
}
