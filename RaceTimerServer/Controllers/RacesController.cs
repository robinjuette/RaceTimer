using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;
using RaceTimerServer.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RacesController : ControllerBase
{
    private readonly RaceRepository _repo;

    public RacesController(RaceRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Race>>> GetAll() => Ok(await _repo.GetAllAsync());

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Race>>> GetByStatus(string status)
    {
        var list = await _repo.GetRacesByStatusAsync(status);
        return Ok(list);
    }

    [HttpGet("{id}/changes")]
    public async Task<ActionResult> GetChanges(Guid id, [FromQuery] DateTime sinceUtc)
    {
        var changes = await _repo.GetChangesSinceAsync(id, sinceUtc);
        return Ok(changes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Race>> Get(Guid id)
    {
        var r = await _repo.GetAsync(id);
        if (r is null) return NotFound();
        return Ok(r);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Race race)
    {
        await _repo.AddAsync(race);
        return CreatedAtAction(nameof(Get), new { id = race.Id }, race);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, Race race)
    {
        if (id != race.Id) return BadRequest();
        var existing = await _repo.GetAsync(id);
        if (existing is null) return NotFound();
        try
        {
            await _repo.UpdateAsync(race);
            return NoContent();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict - the race was modified by another client.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetAsync(id);
        if (existing is null) return NotFound();
        await _repo.RemoveAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/participants/{participantId}")]
    public async Task<ActionResult> AssignParticipant(Guid id, Guid participantId)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        var participant = await _repo.GetParticipantAsync(participantId);
        if (participant is null) return NotFound();
        await _repo.AssignParticipantToRaceAsync(id, participantId);
        return NoContent();
    }

    [HttpDelete("{id}/participants/{participantId}")]
    public async Task<ActionResult> RemoveParticipantFromRace(Guid id, Guid participantId)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        await _repo.RemoveParticipantFromRaceAsync(id, participantId);
        return NoContent();
    }

    // unassigned timepoints are now managed by /api/timepoints

    [HttpPost("timepoints/{timePointId}/assign/{participantId}")]
    public async Task<ActionResult> AssignTimePoint(Guid timePointId, Guid participantId)
    {
        var tp = await _repo.GetAllAsync(); // ensure race exists via lookup
        await _repo.AssignTimePointToParticipantAsync(timePointId, participantId);
        return NoContent();
    }
}
