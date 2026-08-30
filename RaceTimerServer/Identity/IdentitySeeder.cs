using Microsoft.AspNetCore.Identity;

namespace RaceTimerServer.Identity;

public static class IdentitySeeder
{
    public static readonly string[] Roles = ["Administrator", "RaceManager", "Official", "Viewer", "Participant"];

    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
