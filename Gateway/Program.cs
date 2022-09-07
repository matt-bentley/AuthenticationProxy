using Gateway;
using Gateway.Interfaces.Services;
using Gateway.Services;
using Gateway.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NMica.SecurityProxy.Middleware.Transforms;

var builder = WebApplication.CreateBuilder(args);

var identityServerSettings = new IdentityServerSettings();
builder.Configuration.GetSection("IdentityServer").Bind(identityServerSettings);
builder.Services.AddSingleton(Options.Create(identityServerSettings));

builder.Services.AddControllers();
builder.Services.AddHttpClient("RefreshToken");
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IAuthenticationSchemeProvider, CustomAuthenticationSchemeProvider>();

if(identityServerSettings.IdentityProvider.Equals("Okta", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IRefreshTokenService, OktaRefreshTokenService>();
}
else
{
    builder.Services.AddHttpClient<IRefreshTokenService, IdentityServerRefreshTokenService>();
}

builder.Services.AddAuthentication()
    .AddOpenIdConnect(options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = identityServerSettings.Authority;
        options.ClientId = identityServerSettings.ClientId;
        options.ClientSecret = identityServerSettings.ClientSecret;
        options.ResponseType = "code";
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");

        // This aligns the life of the cookie with the life of the token.
        // Note this is not the actual expiration of the cookie as seen by the browser.
        // It is an internal value stored in "expires_at".
        options.UseTokenLifetime = false;

        // used to store access_token and refresh_token in cookie
        // needed so that other instances can refresh
        options.SaveTokens = true;

        options.GetClaimsFromUserInfoEndpoint = true;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";

        options.Events = new CookieAuthenticationEvents
        {
            // After the auth cookie has been validated, this event is called.
            // In it we see if the access token is close to expiring.  If it is
            // then we use the refresh token to get a new access token and save them.
            // If the refresh token does not work for some reason then we redirect to 
            // the login screen.
            OnValidatePrincipal = async cookieCtx =>
            {
                var now = DateTimeOffset.UtcNow;
                var expiresAt = cookieCtx.Properties.GetTokenValue("expires_at");
                var accessTokenExpiration = DateTimeOffset.Parse(expiresAt);
                var timeRemaining = accessTokenExpiration.Subtract(now);
                // TODO: Get this from configuration with a fall back value.
                var refreshThresholdSeconds = 60;
                var refreshThreshold = TimeSpan.FromSeconds(refreshThresholdSeconds);

                if (timeRemaining < refreshThreshold)
                {
                    var refreshToken = cookieCtx.Properties.GetTokenValue("refresh_token");
                    var refreshTokenService = cookieCtx.HttpContext.RequestServices.GetRequiredService<IRefreshTokenService>();
                    var response = await refreshTokenService.RefreshAsync(refreshToken);

                    if (!response.IsError)
                    {
                        var expiresInSeconds = response.ExpiresIn;
                        var updatedExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
                        cookieCtx.Properties.UpdateTokenValue("expires_at", updatedExpiresAt.ToString());
                        cookieCtx.Properties.UpdateTokenValue("access_token", response.AccessToken);
                        cookieCtx.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);

                        // Indicate to the cookie middleware that the cookie should be remade (since we have updated it)
                        cookieCtx.ShouldRenew = true;
                    }
                    else
                    {
                        cookieCtx.RejectPrincipal();
                    }
                }
            }
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = identityServerSettings.Authority;
        options.Audience = identityServerSettings.Authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true
        };
    });
builder.Services.AddAuthorization(c => c
    .AddPolicy("authenticated", p => p.RequireAuthenticatedUser()));
builder.Services.AddSingleton<ClaimHeaderAppender>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.RequestTransforms.Add(context.Services.GetRequiredService<ClaimHeaderAppender>());
    });

var app = builder.Build();

app.UseCors(p => p
    .AllowAnyHeader()
    .WithOrigins("https://localhost:8080")
    .AllowAnyMethod()
    .AllowCredentials());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapReverseProxy();
app.Run();