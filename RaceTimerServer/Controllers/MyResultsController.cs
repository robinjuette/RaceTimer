using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceTimerServer.Configuration;
using RaceTimerServer.Identity;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/account/results")]
[Authorize(Policy = AuthorizationPolicies.CanViewOwnResults)]
public sealed class MyResultsController(
    UserManager<RaceTimerUser> users,
    RaceTimerIdentityDbContext identityDb,
    IRaceRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive)
            return Forbid();

        var participantIds = await identityDb.UserParticipants
            .AsNoTracking()
            .Where(x => x.UserId == user.Id && x.RemovedAtUtc == null)
            .Select(x => x.ParticipantId)
            .ToListAsync(cancellationToken);

        if (participantIds.Count == 0)
            return Ok(Array.Empty<MyResultDto>());

        var participantSet = participantIds.ToHashSet();
        var result = new List<MyResultDto>();
        foreach (var race in await repository.GetAllRacesAsync())
        {
            if (race.RaceStatus != RaceStatus.Finished)
                continue;

            var points = await repository.GetRaceParticipantTimePointsForRaceAsync(race.Id);
            result.AddRange(points
                .Where(point => point.ParticipantID is { } participantId && participantSet.Contains(participantId))
                .Select(point => new MyResultDto(
                    race.Id,
                    race.Name,
                    point.ParticipantID!.Value,
                    point.Participant?.DisplayName,
                    point.GetEffectiveTimePoint(),
                    point.PenaltyTime)));
        }

        return Ok(result);
    }
}

public sealed record MyResultDto(
    Guid RaceId,
    string RaceName,
    Guid ParticipantId,
    string? ParticipantName,
    DateTime TimePointUtc,
    TimeSpan? PenaltyTime);
