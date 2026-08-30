using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceTimerServer.Configuration;

namespace RaceTimerServer.Identity;

public sealed record BootstrapResult(bool Succeeded, string? Error);

public sealed class BootstrapService(
    UserManager<RaceTimerUser> users,
    IConfiguration configuration)
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<bool> IsRequiredAsync(CancellationToken cancellationToken = default)
    {
        return (await users.GetUsersInRoleAsync("Administrator")).Count == 0;
    }

    public async Task<BootstrapResult> CreateAdministratorAsync(string token, string userName, string displayName, string password, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var expected = configuration.GetSection("Authentication").Get<AuthenticationOptions>()?.BootstrapToken;
            if (string.IsNullOrWhiteSpace(expected) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(expected), System.Text.Encoding.UTF8.GetBytes(token)))
                return new(false, "Ungültiges Setup-Token.");

            if (!await IsRequiredAsync(cancellationToken))
                return new(false, "Das Setup wurde bereits abgeschlossen.");

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(displayName))
                return new(false, "Benutzername und Anzeigename sind erforderlich.");

            var user = new RaceTimerUser { UserName = userName.Trim(), DisplayName = displayName.Trim(), EmailConfirmed = false };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                return new(false, string.Join(" ", result.Errors.Select(x => x.Description)));

            result = await users.AddToRoleAsync(user, "Administrator");
            if (!result.Succeeded)
                return new(false, string.Join(" ", result.Errors.Select(x => x.Description)));

            return new(true, null);
        }
        finally
        {
            gate.Release();
        }
    }
}
