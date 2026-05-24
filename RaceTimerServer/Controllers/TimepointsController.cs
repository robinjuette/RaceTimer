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

    [HttpPost()]
    public async Task<ActionResult<RaceParticipantTimePoint>> CreateUnassigned([FromBody] DateTime timePointUtc)
    {
        var tp = await _repo.AddUnassignedTimePointAsync(timePointUtc);
        return CreatedAtAction(nameof(Get), new { id = tp.Id }, tp);
    }

    [HttpGet("unassigned")]
    public async Task<ActionResult<IEnumerable<RaceParticipantTimePoint>>> GetUnassigned()
    {
        IEnumerable<RaceParticipantTimePoint>? tps = await _repo.GetUnassignedTimepointsAsync();
        if (tps == null) return StatusCode(500);
        return Ok(tps);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RaceParticipantTimePoint>> Get(Guid id)
    {
        var tp = await _repo.GetTimePointAsync(id);
        if (tp is null) return NotFound();
        return Ok(tp);
    }


    [HttpGet("race/{raceId}")]
    public async Task<ActionResult<IEnumerable<RaceParticipantTimePoint>>> GetForRace(Guid raceId)
    {
        IEnumerable<RaceParticipantTimePoint> tp = await _repo.GetTimePointsForRaceAsync(raceId);
        if (tp is null) return NotFound();
        return Ok(tp);
    }

    [HttpPost("{timePointId}/assign/{participantId}")]
    public async Task<ActionResult> AssignTimepoint(Guid timePointId, Guid participantId)
    {
        await _repo.AssignTimePointToParticipantAsync(timePointId, participantId);
        return NoContent();
    }
}
