using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway;

[Controller]
public class LoginController : Controller
{
    [HttpGet("/login")]
    [Authorize]
    public IActionResult Login(string returnUrl)
    {
        if (returnUrl != null)
        {
            return Redirect(returnUrl);
        }
        return Ok(User.Identity?.Name);
    }

    [HttpGet("/logout")]
    [Authorize]
    public IActionResult Logout(string returnUrl)
    {
        return new SignOutResult(new []
        {
            OpenIdConnectDefaults.AuthenticationScheme, 
            CookieAuthenticationDefaults.AuthenticationScheme
        }, 
        new AuthenticationProperties()
        {
            RedirectUri = returnUrl
        });
    }


    [HttpGet("/whoami")]
    public List<ClaimRecord> WhoAmI()
    {
        return User.Claims.Select(x => new ClaimRecord(x.Type, x.Value)).ToList();
    }

    [HttpGet("/session")]
    public async Task<IDictionary<string, string>> Session()
    {
        return (await HttpContext.AuthenticateAsync()).Properties.Items;
    }

    [HttpGet("/token")]
    public async Task<IActionResult> Token()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        return Ok(accessToken);
    }

    public record ClaimRecord(string Type, object Value);
}