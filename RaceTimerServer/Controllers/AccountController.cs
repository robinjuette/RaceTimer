using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using RaceTimerServer.Identity;

namespace RaceTimerServer.Controllers;

[AllowAnonymous]
public sealed class AccountController(
    SignInManager<RaceTimerUser> signInManager,
    UserManager<RaceTimerUser> userManager,
    IAntiforgery antiforgery) : Controller
{
    [HttpGet("account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
        return Content($"<form method=post action='/account/login'><input type=hidden name=__RequestVerificationToken value='{System.Net.WebUtility.HtmlEncode(token)}'/><input type=hidden name=returnUrl value='{System.Net.WebUtility.HtmlEncode(returnUrl)}'/><label>Benutzername <input name=userName /></label><label>Passwort <input name=password type=password /></label><button type=submit>Anmelden</button></form>", "text/html");
    }

    [HttpPost("account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl = null)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null || !user.IsActive)
            return Unauthorized("Anmeldung fehlgeschlagen.");
        var result = await signInManager.PasswordSignInAsync(user, password, false, true);
        if (!result.Succeeded)
            return Unauthorized("Anmeldung fehlgeschlagen.");
        user.LastLoginUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return LocalRedirect(returnUrl ?? "/");
    }
}
