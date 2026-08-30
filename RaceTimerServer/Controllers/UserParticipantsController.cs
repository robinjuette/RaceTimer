using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceTimerServer.Configuration;
using RaceTimerServer.Identity;
using RaceTimer.Shared.Services;

namespace RaceTimerServer.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/participants")]
[Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
public sealed class UserParticipantsController(
    RaceTimerIdentityDbContext db,
    UserManager<RaceTimerUser> users,
    IRaceRepository races) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
        => Ok(await db.UserParticipants.AsNoTracking()
            .Where(x => x.UserId == userId && x.RemovedAtUtc == null)
            .Select(x => new { x.UserId, x.ParticipantId, x.CreatedAtUtc, x.CreatedByUserId })
            .ToListAsync(cancellationToken));

    [HttpPost("{participantId:guid}")]
    public async Task<IActionResult> Add(Guid userId, Guid participantId, CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(userId.ToString()) is null)
            return NotFound("Benutzer nicht gefunden.");
        if (await races.GetParticipantAsync(participantId) is null)
            return NotFound("Teilnehmer nicht gefunden.");
        if (await db.UserParticipants.AnyAsync(x => x.UserId == userId && x.ParticipantId == participantId && x.RemovedAtUtc == null, cancellationToken))
            return Conflict("Die Zuordnung existiert bereits.");

        var actor = Guid.Parse(users.GetUserId(User)!);
        db.UserParticipants.Add(new RaceTimerUserParticipant { UserId = userId, ParticipantId = participantId, CreatedByUserId = actor });
        db.UserParticipantAudits.Add(new UserParticipantAudit { UserId = userId, ParticipantId = participantId, ActorUserId = actor, Action = "Created" });
        await db.SaveChangesAsync(cancellationToken);
        return Created();
    }

    [HttpDelete("{participantId:guid}")]
    public async Task<IActionResult> Remove(Guid userId, Guid participantId, CancellationToken cancellationToken)
    {
        var relation = await db.UserParticipants.FirstOrDefaultAsync(x => x.UserId == userId && x.ParticipantId == participantId && x.RemovedAtUtc == null, cancellationToken);
        if (relation is null) return NotFound();
        var actor = Guid.Parse(users.GetUserId(User)!);
        relation.RemovedAtUtc = DateTime.UtcNow;
        relation.RemovedByUserId = actor;
        db.UserParticipantAudits.Add(new UserParticipantAudit { UserId = userId, ParticipantId = participantId, ActorUserId = actor, Action = "Removed" });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}