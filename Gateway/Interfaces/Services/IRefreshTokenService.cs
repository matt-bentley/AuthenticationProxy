using IdentityModel.Client;

namespace Gateway.Interfaces.Services
{
    public interface IRefreshTokenService
    {
        Task<TokenResponse> RefreshAsync(string refreshToken);
    }
}
