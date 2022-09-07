using Gateway;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.IdentityModel.Tokens;
using NMica.SecurityProxy.Middleware.Transforms;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var authority = builder.Configuration["Authority"];
var requireHttpsAuthority = authority.StartsWith("https");

builder.Services.AddControllers();
builder.Services.AddHttpClient("RefreshToken");
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IAuthenticationSchemeProvider, CustomAuthenticationSchemeProvider>();
builder.Services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();
builder.Services.AddAuthentication()
    .AddOpenIdConnect(config =>
    {
        config.RequireHttpsMetadata = requireHttpsAuthority;
        config.SignInScheme  = CookieAuthenticationDefaults.AuthenticationScheme;
        config.Authority = authority;
        config.ClientId = "gui";
        config.ClientSecret = "password";
        config.ResponseType = "code";
        config.Scope.Add("openid");
        config.Scope.Add("profile");
        config.Scope.Add("offline_access");
        config.ClaimActions.Add(new DeleteClaimAction("s_hash"));
        config.ClaimActions.Add(new DeleteClaimAction("sid"));
        config.ClaimActions.Add(new DeleteClaimAction("auth_time"));
        config.ClaimActions.Add(new DeleteClaimAction("amr"));

        // This aligns the life of the cookie with the life of the token.
        // Note this is not the actual expiration of the cookie as seen by the browser.
        // It is an internal value stored in "expires_at".
        config.UseTokenLifetime = false;

        // used to store access_token and refresh_token in cookie
        // needed so that other instances can refresh
        config.SaveTokens = true;

        config.GetClaimsFromUserInfoEndpoint = true;
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
                    var clientFactory = cookieCtx.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var client = clientFactory.CreateClient("RefreshToken");
                    var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
                    {
                        Address = $"{authority}/connect/token",
                        ClientId = "gui",
                        ClientSecret = "password",
                        RefreshToken = refreshToken
                    });

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
        options.Authority = authority;
        options.Audience = authority;
        options.RequireHttpsMetadata = requireHttpsAuthority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            NameClaimType = ClaimTypes.Name
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