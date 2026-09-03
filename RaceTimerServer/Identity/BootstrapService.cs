using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceTimerServer.Configuration;

namespace RaceTimerServer.Identity;

public sealed record BootstrapResult(bool Succeeded, string? Error);

public sealed class BootstrapService(
    UserManager<RaceTimerUser> users,
    IConfiguration configuration,
    RaceTimerIdentityDbContext db,
    ILogger<BootstrapService> logger)
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
            {
                logger.LogWarning("Administrator-Setup mit ungültigem Token abgewiesen.");
                return new(false, "Ungültiges Setup-Token.");
            }

            if (!await IsRequiredAsync(cancellationToken))
            {
                logger.LogInformation("Administrator-Setup abgewiesen, da bereits ein Administrator existiert.");
                return new(false, "Das Setup wurde bereits abgeschlossen.");
            }

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(password))
                return new(false, "Benutzername und Anzeigename sind erforderlich.");

            userName = userName.Trim();
            displayName = displayName.Trim();
            if (userName.Length > 256 || displayName.Length > 200)
                return new(false, "Benutzername oder Anzeigename ist zu lang.");

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var user = new RaceTimerUser { UserName = userName, DisplayName = displayName, EmailConfirmed = false };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                return new(false, string.Join(" ", result.Errors.Select(x => x.Description)));

            result = await users.AddToRoleAsync(user, "Administrator");
            if (!result.Succeeded)
                return new(false, string.Join(" ", result.Errors.Select(x => x.Description)));

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Administrator-Setup erfolgreich abgeschlossen.");
            return new(true, null);
        }
        finally
        {
            gate.Release();
        }
    }
}
