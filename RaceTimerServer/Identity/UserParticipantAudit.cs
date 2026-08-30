namespace RaceTimerServer.Identity;

public sealed class UserParticipantAudit
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ParticipantId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
