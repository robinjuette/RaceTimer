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
        await _repo.UpdateAsync(race);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await _repo.GetAsync(id);
        if (existing is null) return NotFound();
        await _repo.RemoveAsync(id);
        return NoContent();
    }
}
