using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using RaceTimerServer.Identity;

namespace RaceTimerServer.Controllers;

[ApiController]
public sealed class AuthorizationController(
    UserManager<RaceTimerUser> users) : ControllerBase
{
    [HttpGet("connect/authorize")]
    [HttpPost("connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest(HttpContext) ?? throw new InvalidOperationException("OIDC-Anfrage fehlt.");
        if (!(User.Identity?.IsAuthenticated ?? false))
            return Challenge(IdentityConstants.ApplicationScheme);

        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive)
            return Forbid(IdentityConstants.ApplicationScheme);

        var principal = await ClaimsPrincipalFactory.CreateAsync(users, user);
        principal.SetScopes(request.GetScopes());
        principal.SetResources("racetimer-api");
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ExchangeToken(CancellationToken cancellationToken)
    {
        var request = Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest(HttpContext)
            ?? throw new InvalidOperationException("OIDC-Anfrage fehlt.");
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            return BadRequest(new { error = "unsupported_grant_type" });

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = result.Principal?.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = subject is null ? null : await users.FindByIdAsync(subject);
        if (user is null || !user.IsActive)
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var principal = await ClaimsPrincipalFactory.CreateAsync(users, user);
        principal.SetScopes(result.Principal?.GetScopes() ?? request.GetScopes());
        principal.SetResources("racetimer-api");
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("connect/logout")]
    [HttpPost("connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
