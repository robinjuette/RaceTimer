using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;
using RaceTimerServer.Configuration;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/public/events")]
[AllowAnonymous]
public sealed class PublicEventsController(
    IRaceRepository repository,
    IOptions<PublicAccessOptions> options) : ControllerBase
{
    [HttpGet("live")]
    public async Task<IActionResult> GetLiveEvents(CancellationToken cancellationToken)
    {
        if (!options.Value.EventDiscovery || !options.Value.LiveEvents)
            return NotFound();

        var races = await repository.GetRacesByStatusAsync(RaceStatus.Running);
        return Ok(races.Select(r => new PublicEventDto(r.Id, r.Name, r.StartTimeUTC)));
    }

    [HttpGet("{id:guid}/live")]
    public async Task<IActionResult> GetLiveEvent(Guid id, CancellationToken cancellationToken)
    {
        if (!options.Value.LiveEvents)
            return NotFound();

        var race = await repository.GetRaceAsync(id);
        return race is { RaceStatus: RaceStatus.Running }
            ? Ok(new PublicEventDto(race.Id, race.Name, race.StartTimeUTC))
            : NotFound();
    }

    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> GetResults(Guid id, CancellationToken cancellationToken)
    {
        if (!options.Value.FinishedResults)
            return NotFound();

        var race = await repository.GetRaceAsync(id);
        if (race is null || race.RaceStatus != RaceStatus.Finished)
            return NotFound();

        var points = await repository.GetRaceParticipantTimePointsForRaceAsync(id);
        return Ok(points.Select(point => new PublicResultDto(
            options.Value.ParticipantDetails ? point.ParticipantID : null,
            options.Value.ParticipantDetails ? point.Participant?.DisplayName : null,
            point.GetEffectiveTimePoint(),
            point.PenaltyTime)));
    }
}

public sealed record PublicEventDto(Guid Id, string Name, DateTime? StartTimeUtc);
public sealed record PublicResultDto(Guid? ParticipantId, string? ParticipantName, DateTime TimePointUtc, TimeSpan? PenaltyTime);
