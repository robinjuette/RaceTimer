using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;
using RaceTimerServer.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimepointsController : ControllerBase
{
    private readonly RaceRepository _repo;

    public TimepointsController(RaceRepository repo)
    {
        _repo = repo;
    }

    [HttpPost("unassigned")]
    public async Task<ActionResult<RaceParticipantTimePoint>> CreateUnassigned([FromBody] DateTime timePointUtc)
    {
        var tp = await _repo.AddUnassignedTimePointAsync(timePointUtc);
        return CreatedAtAction(nameof(Get), new { id = tp.Id }, tp);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RaceParticipantTimePoint>> Get(Guid id)
    {
        var tp = await _repo.GetTimePointAsync(id);
        if (tp is null) return NotFound();
        return Ok(tp);
    }

    [HttpPost("{timePointId}/assign/participant/{participantId}")]
    public async Task<ActionResult> AssignToParticipant(Guid timePointId, Guid participantId)
    {
        await _repo.AssignTimePointToParticipantAsync(timePointId, participantId);
        return NoContent();
    }

    [HttpPost("{timePointId}/assign/race/{raceId}")]
    public async Task<ActionResult> AssignToRace(Guid timePointId, Guid raceId)
    {
        await _repo.AssignTimePointToRaceAsync(timePointId, raceId);
        return NoContent();
    }
}
