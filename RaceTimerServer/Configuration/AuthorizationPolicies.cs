using Microsoft.AspNetCore.Authorization;

namespace RaceTimerServer.Configuration;

public static class AuthorizationPolicies
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanManageRaces = nameof(CanManageRaces);
    public const string CanManageParticipants = nameof(CanManageParticipants);
    public const string CanCorrectTimePoints = nameof(CanCorrectTimePoints);
    public const string CanViewAllResults = nameof(CanViewAllResults);
    public const string CanViewOwnResults = nameof(CanViewOwnResults);
    public const string CanViewPublicLiveEvents = nameof(CanViewPublicLiveEvents);

    public static void AddRaceTimerPolicies(AuthorizationOptions options, bool authenticationEnabled)
    {
        if (!authenticationEnabled)
        {
            foreach (var name in new[] { CanManageUsers, CanManageRaces, CanManageParticipants, CanCorrectTimePoints, CanViewAllResults, CanViewOwnResults, CanViewPublicLiveEvents })
                options.AddPolicy(name, p => p.RequireAssertion(_ => true));
            return;
        }
        options.AddPolicy(CanManageUsers, p => p.RequireRole("Administrator"));
        options.AddPolicy(CanManageRaces, p => p.RequireRole("Administrator", "RaceManager"));
        options.AddPolicy(CanManageParticipants, p => p.RequireRole("Administrator", "RaceManager"));
        options.AddPolicy(CanCorrectTimePoints, p => p.RequireRole("Administrator", "Official"));
        options.AddPolicy(CanViewAllResults, p => p.RequireRole("Administrator", "Official", "RaceManager", "Viewer"));
        options.AddPolicy(CanViewOwnResults, p => p.RequireRole("Administrator", "Participant", "Viewer", "Official", "RaceManager"));
        options.AddPolicy(CanViewPublicLiveEvents, p => p.RequireRole("Administrator", "RaceManager", "Official", "Viewer", "Participant"));
    }
}
