using Microsoft.AspNetCore.Mvc;
using RaceTimer.Shared.Models;

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
    public ActionResult<IEnumerable<Race>> GetAll() => Ok(_repo.GetAll());

    [HttpGet("{id}")]
    public ActionResult<Race> Get(Guid id)
    {
        var r = _repo.Get(id);
        if (r is null) return NotFound();
        return Ok(r);
    }

    [HttpPost]
    public ActionResult Create(Race race)
    {
        _repo.Add(race);
        return CreatedAtAction(nameof(Get), new { id = race.Id }, race);
    }

    [HttpPut("{id}")]
    public ActionResult Update(Guid id, Race race)
    {
        if (id != race.Id) return BadRequest();
        var existing = _repo.Get(id);
        if (existing is null) return NotFound();
        _repo.Update(race);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(Guid id)
    {
        var existing = _repo.Get(id);
        if (existing is null) return NotFound();
        _repo.Remove(id);
        return NoContent();
    }
}
