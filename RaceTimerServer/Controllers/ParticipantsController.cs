using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;
using RaceTimerServer.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParticipantsController : ControllerBase
{
    private readonly RaceRepository _repo;

    public ParticipantsController(RaceRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Participant>>> GetAll()
    {
        var participants = await _repo.GetParticipantsAsync();
        return Ok(participants);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Participant>> Get(Guid id)
    {
        var p = await _repo.GetParticipantAsync(id);
        if (p is null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Participant participant)
    {
        await _repo.AddParticipantAsync(participant);
        return CreatedAtAction(nameof(Get), new { id = participant.Id }, participant);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, Participant participant)
    {
        if (id != participant.Id) return BadRequest();
        var existing = await _repo.GetParticipantAsync(id);
        if (existing is null) return NotFound();
        await _repo.UpdateParticipantAsync(participant);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetParticipantAsync(id);
        if (existing is null) return NotFound();
        await _repo.RemoveParticipantAsync(id);
        return NoContent();
    }
}
