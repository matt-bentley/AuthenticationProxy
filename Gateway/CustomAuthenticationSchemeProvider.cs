using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Gateway
{
    public class CustomAuthenticationSchemeProvider : AuthenticationSchemeProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthenticationSchemeProvider(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuthenticationOptions> options)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsBearer
        {
            get
            {
                string authorization = _httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization];
                return !string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ");
            }
        }

        public override async Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync()
        {
            var schemeName = IsBearer ? JwtBearerDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
            return await GetSchemeAsync(schemeName);
        }

        public override async Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync()
        {
            var schemeName = IsBearer ? JwtBearerDefaults.AuthenticationScheme : OpenIdConnectDefaults.AuthenticationScheme;
            return await GetSchemeAsync(schemeName);
        }
    }
}
