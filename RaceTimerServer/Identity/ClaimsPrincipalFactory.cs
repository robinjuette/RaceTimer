using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace RaceTimerServer.Identity;

public static class ClaimsPrincipalFactory
{
    public static async Task<ClaimsPrincipal> CreateAsync(UserManager<RaceTimerUser> userManager, RaceTimerUser user)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.AddClaim(OpenIddictConstants.Claims.Name, user.UserName ?? "");
        identity.AddClaim(OpenIddictConstants.Claims.PreferredUsername, user.UserName ?? "");
        identity.AddClaim(OpenIddictConstants.Claims.Profile, user.DisplayName);
        foreach (var role in await userManager.GetRolesAsync(user))
            identity.AddClaim(OpenIddictConstants.Claims.Role, role);

        var principal = new ClaimsPrincipal(identity);
        principal.SetResources("racetimer-api");
        return principal;
    }
}
