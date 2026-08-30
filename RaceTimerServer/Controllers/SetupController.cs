using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceTimerServer.Identity;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/setup")]
[AllowAnonymous]
public sealed class SetupController(BootstrapService bootstrap) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
        => Ok(new { required = await bootstrap.IsRequiredAsync(cancellationToken) });

    [HttpPost("administrator")]
    public async Task<IActionResult> CreateAdministrator([FromBody] SetupRequest request, CancellationToken cancellationToken)
    {
        var result = await bootstrap.CreateAdministratorAsync(request.Token, request.UserName, request.DisplayName, request.Password, cancellationToken);
        return result.Succeeded ? Ok() : BadRequest(new { error = result.Error });
    }
}

public sealed record SetupRequest(string Token, string UserName, string DisplayName, string Password);
