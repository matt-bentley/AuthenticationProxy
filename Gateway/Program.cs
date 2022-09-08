using Gateway;
using Gateway.Interfaces.Services;
using Gateway.Services;
using Gateway.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using Gateway.Authentication;
using Gateway.Infrastructure.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

var identityServerSettings = new IdentityServerSettings();
builder.Configuration.GetSection("IdentityServer").Bind(identityServerSettings);
builder.Services.AddSingleton(Options.Create(identityServerSettings));

builder.Services.AddControllers();
builder.Services.AddHttpClient("RefreshToken");
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IAuthenticationSchemeProvider, CustomAuthenticationSchemeProvider>();

builder.Services.AddDataProtection()
    .DisableAutomaticKeyGeneration()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Join(Directory.GetCurrentDirectory(), "DataProtection")));

if (identityServerSettings.IdentityProvider.Equals("Okta", StringComparison.OrdinalIgnoreCase))
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
        options.Audience = identityServerSettings.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddTransient<IAuthorizationHandler, AdminRequirementHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();

    options.AddPolicy("admin", p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new AdminRequirement(builder.Configuration.GetSection("Administrators").Get<string[]>()));
    });
});

builder.Services.AddSingleton<ClaimHeaderAppender>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        context.RequestTransforms.Add(context.Services.GetRequiredService<ClaimHeaderAppender>());
    });

builder.Services.AddHealthChecks()
                .AddLiveness("gateway");

// this is needed to forward the host headers when reverse proxied through load balancers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:10.0.0.0"), 104));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:192.168.0.0"), 112));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:172.16.0.0"), 108));
});

var app = builder.Build();

// Required to forward headers from load balancers and reverse proxies
app.UseForwardedHeaders();

app.UseCors(p => p
    .AllowAnyHeader()
    .WithOrigins("https://localhost:8080")
    .AllowAnyMethod()
    .AllowCredentials());

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/gateway/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self")
}).AllowAnonymous();
app.MapHealthChecks("/gateway/healthz", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapReverseProxy();
app.Run();