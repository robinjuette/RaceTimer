using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceTimer.Shared.Data;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly RaceTimerDbContext _db;

    public HealthController(RaceTimerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public ActionResult<object> Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpPost("database")]
    public async Task<ActionResult<object>> TestDatabase()
    {
        try
        {
            await _db.Database.CanConnectAsync();
            return Ok(new { status = "connected", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("migrate")]
    public async Task<ActionResult<object>> MigrateDatabase()
    {
        try
        {
            await _db.Database.MigrateAsync();
            return Ok(new { status = "migrated", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}