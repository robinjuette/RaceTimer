using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceTimerController : ControllerBase
{
    private readonly IRaceRepository _repo;

    public RaceTimerController(IRaceRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("races")]
    public async Task<IActionResult> GetRaces()
    {
        var races = await _repo.GetAllRacesAsync();
        return Ok(races);
    }

    [HttpGet("races/{id}")]
    public async Task<IActionResult> GetRace(Guid id)
    {
        var race = await _repo.GetRaceAsync(id);
        if (race == null) return NotFound();
        return Ok(race);
    }

    [HttpPost("races")]
    public async Task<IActionResult> CreateRace([FromBody] Race race)
    {
        var created = await _repo.AddRaceAsync(race.Name);
        if (created == null) return Conflict();
        return CreatedAtAction(nameof(GetRace), new { id = created.Id }, created);
    }

    [HttpPut("races/{id}")]
    public async Task<IActionResult> UpdateRace(Guid id, [FromBody] Race race)
    {
        if (id != race.Id) return BadRequest();
        await _repo.UpdateRaceAsync(race);
        return NoContent();
    }

    [HttpDelete("races/{id}")]
    public async Task<IActionResult> DeleteRace(Guid id)
    {
        var ok = await _repo.DeleteRaceAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
