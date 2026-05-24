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

    [HttpGet("running")]
    public async Task<ActionResult<IEnumerable<Race>>> GetRunning()
    {
        var list = await _repo.GetRacesByStatusAsync("running");
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
    public async Task<ActionResult<RaceParticipant>> AssignParticipant(Guid id, Guid participantId)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        var participant = await _repo.GetParticipantAsync(participantId);
        if (participant is null) return NotFound();
        return await _repo.AssignParticipantToRaceAsync(id, participantId);
    }

    [HttpGet("{id}/participants/")]
    public async Task<ActionResult<IEnumerable<RaceParticipant>>> GetParticipants(Guid id)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        return Ok(await _repo.GetRaceParticipantsAsync(id));
    }

    [HttpDelete("{id}/participants/{participantId}")]
    public async Task<ActionResult> RemoveParticipantFromRace(Guid id, Guid participantId)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        await _repo.RemoveParticipantFromRaceAsync(id, participantId);
        return NoContent();
    }

    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartRace(Guid id, IEnumerable<Guid> participants)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        if (race.FinishDateTimeUTC != null) return BadRequest();
        DateTime startDT = DateTime.UtcNow;

        if (race.StartTimeUTC == null)
        {
            race.StartTimeUTC = startDT;
            await _repo.UpdateAsync(race);
        }

        foreach(Guid guid in participants)
        {
            var tp = await _repo.AddUnassignedTimePointAsync(startDT);
            await _repo.AssignTimePointToParticipantAsync(tp.Id, guid);
        }

        return Ok();
    }

    [HttpPost("{id}/finish")]
    public async Task<ActionResult> FinishRace(Guid id)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        if (race.StartTimeUTC is null) return BadRequest("Race not started");
        if (race.FinishDateTimeUTC is not null) return BadRequest("Race already finished");

        race.FinishDateTimeUTC = DateTime.UtcNow;
        await _repo.UpdateAsync(race);
        return Ok();
    }
    [HttpGet("{id}/timepoints")]
    public async Task<ActionResult<IEnumerable<RaceTimePoint>>> GetRaceTimePoints(Guid id)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();
        return Ok(race.RaceTimePoints.OrderBy(tp => tp.Index));
    }

    [HttpPost("{id}/timepoint")]
    public async Task<ActionResult<RaceTimePoint>> AddRaceTimePoint(Guid id, [FromBody] RaceTimePoint timePoint)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();

        timePoint.Id = Guid.NewGuid();
        timePoint.RaceID = id;

        var result = await _repo.AddTimePointAsync(id, timePoint);
        return CreatedAtAction(nameof(GetRaceTimePoints), new { id }, result);
    }

    [HttpDelete("{id}/timepoints/{timePointId}")]
    public async Task<ActionResult> DeleteRaceTimePoint(Guid id, Guid timePointId)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();

        await _repo.RemoveTimePointAsync(id, timePointId);
        return NoContent();
    }

    [HttpPatch("{id}/timepoint/{timePointID}")]
    public async Task<ActionResult<RaceTimePoint>> UpdateRaceTimePoint(Guid id, Guid timePointID, RaceTimePoint raceTimePoint)
    {
        var race = await _repo.GetAsync(id);
        if (race is null) return NotFound();

        raceTimePoint.Id = timePointID;
        raceTimePoint.RaceID = id;

        var result = await _repo.UpdateTimePointAsync(id, raceTimePoint);
        return Ok(result);
    }
}