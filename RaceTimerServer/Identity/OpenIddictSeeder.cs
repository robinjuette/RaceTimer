using OpenIddict.Abstractions;
using RaceTimerServer.Configuration;

namespace RaceTimerServer.Identity;

public static class OpenIddictSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var options = configuration.GetSection("Authentication").Get<AuthenticationOptions>() ?? new();
        if (await manager.FindByClientIdAsync(options.WebClientId) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = options.WebClientId,
                ClientSecret = options.WebClientSecret,
                DisplayName = "RaceTimer Web",
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                RedirectUris = { new Uri("https://localhost:5002/signin-oidc"), new Uri("http://localhost:8088/signin-oidc") },
                PostLogoutRedirectUris = { new Uri("https://localhost:5002/signout-callback-oidc"), new Uri("http://localhost:8088/signout-callback-oidc") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "racetimer.read",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "racetimer.manage"
                }
            });
        }
    }
}
